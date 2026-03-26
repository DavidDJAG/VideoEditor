namespace VideoEditor.Domain.Models;

public sealed record OperationParameters(
    string InputPath,
    string? OutputPath,
    TimeSpan? Start,
    TimeSpan? End,
    IReadOnlyList<string> AdditionalInputs,
    IReadOnlyDictionary<string, string> Flags,
    EncodingProfile? EncodingProfile,
    IReadOnlyList<string>? ConcatInputs = null);
