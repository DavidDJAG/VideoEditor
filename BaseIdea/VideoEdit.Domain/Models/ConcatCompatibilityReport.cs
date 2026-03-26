namespace VideoEdit.Domain.Models;

public sealed class ConcatCompatibilityReport
{
    public bool IsCompatible => Issues.Count == 0;

    public List<ConcatCompatibilityIssue> Issues { get; init; } = [];

    public List<MediaProbeResult> Probes { get; init; } = [];

    public string Summary =>
        IsCompatible
            ? "Los archivos son compatibles para concatenacion por copia."
            : string.Join(Environment.NewLine, Issues.Select(issue => $"{issue.Category}: {Path.GetFileName(issue.FilePath)} - {issue.Detail}"));
}
