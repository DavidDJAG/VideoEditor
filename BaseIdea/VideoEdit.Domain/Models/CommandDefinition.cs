namespace VideoEdit.Domain.Models;

public sealed class CommandDefinition
{
    public string FileName { get; init; } = string.Empty;

    public string Arguments { get; init; } = string.Empty;

    public string CommandText { get; init; } = string.Empty;
}
