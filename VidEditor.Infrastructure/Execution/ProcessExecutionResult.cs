namespace VidEditor.Infrastructure.Execution;

public sealed record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);
