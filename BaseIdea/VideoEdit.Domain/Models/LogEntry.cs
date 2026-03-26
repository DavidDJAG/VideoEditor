namespace VideoEdit.Domain.Models;

public sealed class LogEntry
{
    public Guid JobId { get; init; }

    public string JobName { get; init; } = string.Empty;

    public string CommandText { get; init; } = string.Empty;

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public int ExitCode { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public bool Success { get; init; }
}
