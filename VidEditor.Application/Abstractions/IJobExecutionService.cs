using VidEditor.Domain.Models;

namespace VidEditor.Application.Abstractions;

public interface IJobExecutionService
{
    Task<JobExecutionArtifact> ExecuteAsync(MediaJob job, CancellationToken cancellationToken = default);
}
