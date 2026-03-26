namespace VideoEdit.Domain.Models;

public sealed class ProcessExecutionResult
{
    public int ExitCode { get; init; }

    public bool WasCancelled { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;
}
