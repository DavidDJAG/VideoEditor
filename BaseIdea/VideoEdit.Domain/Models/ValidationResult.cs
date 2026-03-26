namespace VideoEdit.Domain.Models;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = [];

    public static ValidationResult Success() => new();
}
