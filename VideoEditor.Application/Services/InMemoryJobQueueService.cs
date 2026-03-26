using System.Collections.Concurrent;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Services;

public sealed class InMemoryJobQueueService : IJobQueueService
{
    private readonly ConcurrentDictionary<Guid, MediaJob> _jobs = new();

    public IReadOnlyCollection<MediaJob> GetAll() => _jobs.Values.ToArray();

    public MediaJob Enqueue(MediaJob job)
    {
        _jobs[job.Id] = job;
        return job;
    }

    public bool TryUpdate(MediaJob job)
    {
        while (_jobs.TryGetValue(job.Id, out var current))
        {
            if (_jobs.TryUpdate(job.Id, job, current))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryDequeue(Guid jobId, out MediaJob? job)
    {
        var removed = _jobs.TryRemove(jobId, out var local);
        job = local;
        return removed;
    }
}
