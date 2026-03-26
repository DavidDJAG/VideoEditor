using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class QueueViewModel
{
    private readonly IJobQueueService _jobQueueService;

    public QueueViewModel(IJobQueueService jobQueueService)
    {
        _jobQueueService = jobQueueService;
    }

    public IReadOnlyCollection<MediaJob> Jobs => _jobQueueService.GetAll();

    public MediaJob Enqueue(MediaJob job) => _jobQueueService.Enqueue(job);
}
