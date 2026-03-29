using System.Text.Json;
using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.Infrastructure.Services;

public sealed class JsonJobStore : IJobStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _jobsDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public JsonJobStore(string jobsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobsDirectory);

        _jobsDirectory = jobsDirectory;
        Directory.CreateDirectory(_jobsDirectory);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        Directory.CreateDirectory(_jobsDirectory);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<MediaJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var jobs = new List<MediaJob>();
            foreach (var path in Directory.EnumerateFiles(_jobsDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var job = await ReadJobAsync(path, cancellationToken);
                if (job is not null)
                {
                    jobs.Add(job);
                }
            }

            return jobs
                .OrderByDescending(static job => job.CreatedAt)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertAsync(MediaJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ThrowIfDisposed();

        await _gate.WaitAsync(cancellationToken);
        string? tempPath = null;

        try
        {
            Directory.CreateDirectory(_jobsDirectory);

            var destinationPath = GetJobPath(job.Id);
            tempPath = Path.Combine(_jobsDirectory, $"{job.Id:D}.{Guid.NewGuid():N}.tmp");

            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, job, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, destinationPath, overwrite: true);
            tempPath = null;
        }
        finally
        {
            if (tempPath is not null && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            _gate.Release();
        }
    }

    public async Task<MediaJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = GetJobPath(id);
            if (!File.Exists(path))
            {
                return null;
            }

            return await ReadJobAsync(path, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    private string GetJobPath(Guid id)
        => Path.Combine(_jobsDirectory, $"{id:D}.json");

    private static async Task<MediaJob?> ReadJobAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

        try
        {
            return await JsonSerializer.DeserializeAsync<MediaJob>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The job file '{path}' is invalid.", ex);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
