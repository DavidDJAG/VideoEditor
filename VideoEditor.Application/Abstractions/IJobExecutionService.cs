using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IJobExecutionService
{
    Task<JobExecutionArtifact> ExecuteAsync(MediaJob job, CancellationToken cancellationToken = default);
}
