namespace VideoEdit.Domain.Models;

public sealed class MediaJobResult
{
    public bool Success { get; init; }

    public bool Cancelled { get; init; }

    public int ExitCode { get; init; }

    public string CommandText { get; init; } = string.Empty;

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public string? OutputPath { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset FinishedAt { get; init; }
}
