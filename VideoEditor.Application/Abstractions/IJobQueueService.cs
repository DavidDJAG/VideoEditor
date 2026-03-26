using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IJobQueueService
{
    IReadOnlyCollection<MediaJob> GetAll();

    MediaJob Enqueue(MediaJob job);

    bool TryUpdate(MediaJob job);

    bool TryDequeue(Guid jobId, out MediaJob? job);
}
