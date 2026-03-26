namespace VideoEdit.Domain.Models;

public sealed class MediaJob
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public required MediaJobRequest Request { get; init; }

    public MediaJobStatus Status { get; set; } = MediaJobStatus.Pending;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public string? CommandPreview { get; set; }

    public string? LastMessage { get; set; }

    public MediaJobResult? Result { get; set; }
}
