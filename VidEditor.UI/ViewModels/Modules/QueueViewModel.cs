using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.UI.ViewModels.Modules;

public sealed class QueueViewModel
{
    private readonly IJobQueueService _jobQueueService;

    public QueueViewModel(IJobQueueService jobQueueService)
    {
        _jobQueueService = jobQueueService;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => _jobQueueService.InitializeAsync(cancellationToken);

    public Task<IReadOnlyCollection<MediaJob>> GetJobsAsync(CancellationToken cancellationToken = default)
        => _jobQueueService.GetAllAsync(cancellationToken);

    public Task<IReadOnlyCollection<MediaJob>> GetHistoryAsync(JobHistoryFilter filter, CancellationToken cancellationToken = default)
        => _jobQueueService.GetHistoryAsync(filter, cancellationToken);

    public Task<MediaJob> CreateDraftAsync(MediaJob job, CancellationToken cancellationToken = default)
        => _jobQueueService.CreateDraftAsync(job, cancellationToken);

    public Task<MediaJob> EnqueueAsync(MediaJob job, CancellationToken cancellationToken = default)
        => _jobQueueService.EnqueueAsync(job, cancellationToken);

    public Task<bool> PauseAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _jobQueueService.PauseAsync(jobId, cancellationToken);

    public Task<bool> ResumeAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _jobQueueService.ResumeAsync(jobId, cancellationToken);

    public Task<bool> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _jobQueueService.CancelAsync(jobId, cancellationToken);

    public Task<bool> RetryAsync(Guid jobId, CancellationToken cancellationToken = default)
        => _jobQueueService.RetryAsync(jobId, cancellationToken);

    public Task<string> ExportDiagnosticsBundleAsync(string targetDirectory, JobHistoryFilter? filter = null, CancellationToken cancellationToken = default)
        => _jobQueueService.ExportDiagnosticsBundleAsync(targetDirectory, filter, cancellationToken);
}
