using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IJobQueueService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MediaJob>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MediaJob>> GetHistoryAsync(JobHistoryFilter filter, CancellationToken cancellationToken = default);

    Task<MediaJob> CreateDraftAsync(MediaJob job, CancellationToken cancellationToken = default);

    Task<MediaJob> EnqueueAsync(MediaJob job, CancellationToken cancellationToken = default);

    Task<bool> PauseAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<bool> ResumeAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<bool> RetryAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<string> ExportDiagnosticsBundleAsync(string targetDirectory, JobHistoryFilter? filter = null, CancellationToken cancellationToken = default);
}
