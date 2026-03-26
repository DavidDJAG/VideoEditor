namespace VideoEditor.Domain.Models;

public sealed record MediaJob(
    Guid Id,
    string Name,
    string Operation,
    OperationParameters Parameters,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    JobState State,
    double Progress = 0,
    string? Error = null,
    string? OutputPath = null,
    RetryPolicy? RetryPolicy = null,
    int AttemptCount = 0,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? FinishedAt = null,
    JobExecutionArtifact? LastArtifact = null,
    bool IsCancellationRequested = false)
{
    public RetryPolicy EffectiveRetryPolicy => RetryPolicy ?? VideoEditor.Domain.Models.RetryPolicy.Default;
}
