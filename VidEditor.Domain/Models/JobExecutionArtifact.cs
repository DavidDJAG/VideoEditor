namespace VidEditor.Domain.Models;

public sealed record JobExecutionArtifact(
    Guid JobId,
    string CommandLine,
    string StandardOutput,
    string StandardError,
    int? ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    IReadOnlyList<string> OutputFiles)
{
    public TimeSpan Duration => FinishedAt - StartedAt;
}
