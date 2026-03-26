using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface IJobQueueService
{
    event EventHandler<MediaJob>? JobUpdated;

    IReadOnlyList<MediaJob> Jobs { get; }

    void Enqueue(MediaJob job);

    Task CancelActiveJobAsync();
}
