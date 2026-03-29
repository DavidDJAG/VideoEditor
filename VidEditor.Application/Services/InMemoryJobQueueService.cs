using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Channels;
using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.Application.Services;

public sealed class InMemoryJobQueueService : IJobQueueService, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IJobStore _jobStore;
    private readonly IJobExecutionService _jobExecutionService;
    private readonly ConcurrentDictionary<Guid, MediaJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCancellation = new();
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    private readonly SemaphoreSlim _concurrencyGate;
    private readonly CancellationTokenSource _processorCts = new();
    private readonly Task _processorTask;

    public InMemoryJobQueueService(IJobStore jobStore, IJobExecutionService jobExecutionService, int maxConcurrency = 1)
    {
        _jobStore = jobStore;
        _jobExecutionService = jobExecutionService;
        _concurrencyGate = new SemaphoreSlim(Math.Max(1, maxConcurrency), Math.Max(1, maxConcurrency));
        _processorTask = Task.Run(ProcessQueueAsync);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _jobStore.InitializeAsync(cancellationToken);
        var existing = await _jobStore.GetAllAsync(cancellationToken);

        foreach (var job in existing)
        {
            var hydrated = job.State is JobState.Running or JobState.Queued
                ? job with { State = JobState.Queued, Error = "Recovered after application restart." }
                : job;
            _jobs[hydrated.Id] = hydrated;
            if (hydrated.State == JobState.Queued)
            {
                await _queue.Writer.WriteAsync(hydrated.Id, cancellationToken);
            }
        }
    }

    public Task<IReadOnlyCollection<MediaJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<MediaJob> snapshot = _jobs.Values.OrderByDescending(x => x.CreatedAt).ToImmutableArray();
        return Task.FromResult(snapshot);
    }

    public async Task<IReadOnlyCollection<MediaJob>> GetHistoryAsync(JobHistoryFilter filter, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            query = query.Where(job =>
                job.Name.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                job.Operation.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                (job.Error?.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (filter.States is { Count: > 0 })
        {
            query = query.Where(job => filter.States.Contains(job.State));
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(job => job.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(job => job.CreatedAt <= filter.CreatedTo.Value);
        }

        return query.OrderByDescending(x => x.CreatedAt).ToImmutableArray();
    }

    public async Task<MediaJob> CreateDraftAsync(MediaJob job, CancellationToken cancellationToken = default)
    {
        var draft = job with { State = JobState.Draft, RetryPolicy = job.RetryPolicy ?? RetryPolicy.Default };
        _jobs[draft.Id] = draft;
        await _jobStore.UpsertAsync(draft, cancellationToken);
        return draft;
    }

    public async Task<MediaJob> EnqueueAsync(MediaJob job, CancellationToken cancellationToken = default)
    {
        var queued = job with
        {
            State = JobState.Queued,
            RetryPolicy = job.RetryPolicy ?? RetryPolicy.Default,
            Error = null,
            IsCancellationRequested = false
        };

        _jobs[queued.Id] = queued;
        await _jobStore.UpsertAsync(queued, cancellationToken);
        await _queue.Writer.WriteAsync(queued.Id, cancellationToken);
        return queued;
    }

    public async Task<bool> PauseAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job) || job.State != JobState.Queued)
        {
            return false;
        }

        var paused = job with { State = JobState.Paused };
        _jobs[jobId] = paused;
        await _jobStore.UpsertAsync(paused, cancellationToken);
        return true;
    }

    public async Task<bool> ResumeAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job) || job.State != JobState.Paused)
        {
            return false;
        }

        var queued = job with { State = JobState.Queued };
        _jobs[jobId] = queued;
        await _jobStore.UpsertAsync(queued, cancellationToken);
        await _queue.Writer.WriteAsync(jobId, cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        if (_jobCancellation.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
        }

        var cancelled = job with
        {
            State = JobState.Cancelled,
            IsCancellationRequested = true,
            FinishedAt = DateTimeOffset.UtcNow,
            Error = "Cancelled by user."
        };

        _jobs[jobId] = cancelled;
        await _jobStore.UpsertAsync(cancelled, cancellationToken);
        return true;
    }

    public async Task<bool> RetryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job) || (job.State != JobState.Failed && job.State != JobState.Cancelled))
        {
            return false;
        }

        var reset = job with
        {
            State = JobState.Queued,
            Error = null,
            Progress = 0,
            IsCancellationRequested = false,
            StartedAt = null,
            FinishedAt = null
        };

        _jobs[jobId] = reset;
        await _jobStore.UpsertAsync(reset, cancellationToken);
        await _queue.Writer.WriteAsync(jobId, cancellationToken);
        return true;
    }

    public async Task<string> ExportDiagnosticsBundleAsync(string targetDirectory, JobHistoryFilter? filter = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetDirectory);
        var jobs = filter is null
            ? await GetAllAsync(cancellationToken)
            : await GetHistoryAsync(filter, cancellationToken);

        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var tempDir = Path.Combine(targetDirectory, $"diagnostics_{stamp}");
        Directory.CreateDirectory(tempDir);

        var jobsPath = Path.Combine(tempDir, "jobs.json");
        await File.WriteAllTextAsync(jobsPath, JsonSerializer.Serialize(jobs, JsonOptions), cancellationToken);

        var artifactDir = Path.Combine(tempDir, "artifacts");
        Directory.CreateDirectory(artifactDir);
        foreach (var job in jobs.Where(x => x.LastArtifact is not null))
        {
            var artifactPath = Path.Combine(artifactDir, $"{job.Id}.json");
            await File.WriteAllTextAsync(artifactPath, JsonSerializer.Serialize(job.LastArtifact, JsonOptions), cancellationToken);
        }

        var bundlePath = Path.Combine(targetDirectory, $"diagnostics_{stamp}.zip");
        if (File.Exists(bundlePath))
        {
            File.Delete(bundlePath);
        }

        ZipFile.CreateFromDirectory(tempDir, bundlePath, CompressionLevel.Optimal, includeBaseDirectory: false);
        Directory.Delete(tempDir, recursive: true);
        return bundlePath;
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var jobId in _queue.Reader.ReadAllAsync(_processorCts.Token))
            {
                if (!_jobs.TryGetValue(jobId, out var job) || job.State != JobState.Queued)
                {
                    continue;
                }

                await _concurrencyGate.WaitAsync(_processorCts.Token);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RunJobAsync(jobId, _processorCts.Token);
                    }
                    finally
                    {
                        _concurrencyGate.Release();
                    }
                }, _processorCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (!_jobs.TryGetValue(jobId, out var current) || current.State != JobState.Queued)
        {
            return;
        }

        var running = current with { State = JobState.Running, StartedAt = DateTimeOffset.UtcNow, AttemptCount = current.AttemptCount + 1 };
        _jobs[jobId] = running;
        await _jobStore.UpsertAsync(running, cancellationToken);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _jobCancellation[jobId] = linkedCts;

        try
        {
            var artifact = await _jobExecutionService.ExecuteAsync(running, linkedCts.Token);
            var successful = running with
            {
                State = artifact.ExitCode == 0 ? JobState.Succeeded : JobState.Failed,
                LastArtifact = artifact,
                FinishedAt = artifact.FinishedAt,
                OutputPath = artifact.OutputFiles.FirstOrDefault(),
                Progress = 100,
                Error = artifact.ExitCode == 0 ? null : $"Process exited with code {artifact.ExitCode}."
            };
            _jobs[jobId] = successful;
            await _jobStore.UpsertAsync(successful, cancellationToken);

            if (successful.State == JobState.Failed && successful.AttemptCount < successful.EffectiveRetryPolicy.MaxAttempts)
            {
                if (successful.EffectiveRetryPolicy.Delay > TimeSpan.Zero)
                {
                    await Task.Delay(successful.EffectiveRetryPolicy.Delay, cancellationToken);
                }

                var retried = successful with { State = JobState.Queued };
                _jobs[jobId] = retried;
                await _jobStore.UpsertAsync(retried, cancellationToken);
                await _queue.Writer.WriteAsync(jobId, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            if (_jobs.TryGetValue(jobId, out var cancelledSource))
            {
                var cancelled = cancelledSource with
                {
                    State = JobState.Cancelled,
                    FinishedAt = DateTimeOffset.UtcNow,
                    Error = "Cancelled by user."
                };
                _jobs[jobId] = cancelled;
                await _jobStore.UpsertAsync(cancelled, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            if (_jobs.TryGetValue(jobId, out var failedSource))
            {
                var failed = failedSource with
                {
                    State = JobState.Failed,
                    FinishedAt = DateTimeOffset.UtcNow,
                    Error = ex.Message
                };
                _jobs[jobId] = failed;
                await _jobStore.UpsertAsync(failed, CancellationToken.None);
            }
        }
        finally
        {
            _jobCancellation.TryRemove(jobId, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _processorCts.Cancel();
        _queue.Writer.TryComplete();

        try
        {
            await _processorTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _concurrencyGate.Dispose();
        _processorCts.Dispose();
        foreach (var cts in _jobCancellation.Values)
        {
            cts.Dispose();
        }
    }
}
