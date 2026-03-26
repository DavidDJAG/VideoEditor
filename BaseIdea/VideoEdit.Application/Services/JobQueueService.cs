using System.Collections.ObjectModel;
using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Services;

public sealed class JobQueueService : IJobQueueService
{
    private readonly IFfmpegService _ffmpegService;
    private readonly ILogStore _logStore;
    private readonly ObservableCollection<MediaJob> _jobs = [];
    private readonly SemaphoreSlim _signal = new(0);
    private readonly object _sync = new();
    private CancellationTokenSource? _activeJobCts;

    public JobQueueService(IFfmpegService ffmpegService, ILogStore logStore)
    {
        _ffmpegService = ffmpegService;
        _logStore = logStore;
        Jobs = new ReadOnlyObservableCollection<MediaJob>(_jobs);
        _ = Task.Run(ProcessLoopAsync);
    }

    public event EventHandler<MediaJob>? JobUpdated;

    public IReadOnlyList<MediaJob> Jobs { get; }

    public void Enqueue(MediaJob job)
    {
        lock (_sync)
        {
            _jobs.Add(job);
        }

        JobUpdated?.Invoke(this, job);
        _signal.Release();
    }

    public Task CancelActiveJobAsync()
    {
        _activeJobCts?.Cancel();
        return Task.CompletedTask;
    }

    private async Task ProcessLoopAsync()
    {
        while (true)
        {
            await _signal.WaitAsync().ConfigureAwait(false);

            MediaJob? nextJob;
            lock (_sync)
            {
                nextJob = _jobs.FirstOrDefault(job => job.Status == MediaJobStatus.Pending);
            }

            if (nextJob is null)
            {
                continue;
            }

            using var cts = new CancellationTokenSource();
            _activeJobCts = cts;
            nextJob.Status = MediaJobStatus.Running;
            nextJob.StartedAt = DateTimeOffset.Now;
            JobUpdated?.Invoke(this, nextJob);

            try
            {
                var result = await _ffmpegService.ExecuteJobAsync(nextJob, cts.Token).ConfigureAwait(false);
                nextJob.Result = result;
                nextJob.Status = result.Cancelled
                    ? MediaJobStatus.Cancelled
                    : result.Success
                        ? MediaJobStatus.Completed
                        : MediaJobStatus.Error;
                nextJob.LastMessage = result.Cancelled
                    ? "Cancelado por el usuario."
                    : result.Success
                        ? "Completado correctamente."
                        : "FFmpeg devolvió un error.";
                nextJob.FinishedAt = result.FinishedAt;

                await _logStore.AppendAsync(new LogEntry
                {
                    JobId = nextJob.Id,
                    JobName = nextJob.Name,
                    CommandText = result.CommandText,
                    StandardOutput = result.StandardOutput,
                    StandardError = result.StandardError,
                    ExitCode = result.ExitCode,
                    CreatedAt = result.FinishedAt,
                    Success = result.Success
                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                nextJob.Status = MediaJobStatus.Cancelled;
                nextJob.LastMessage = "Cancelado por el usuario.";
                nextJob.FinishedAt = DateTimeOffset.Now;
            }
            catch (Exception ex)
            {
                nextJob.Status = MediaJobStatus.Error;
                nextJob.LastMessage = ex.Message;
                nextJob.FinishedAt = DateTimeOffset.Now;
            }
            finally
            {
                JobUpdated?.Invoke(this, nextJob);
                _activeJobCts = null;
            }
        }
    }
}
