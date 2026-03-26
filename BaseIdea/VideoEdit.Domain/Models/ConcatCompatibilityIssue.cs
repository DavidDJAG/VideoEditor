namespace VideoEdit.Domain.Models;

public sealed class ConcatCompatibilityIssue
{
    public string Category { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public string Detail { get; init; } = string.Empty;
}
