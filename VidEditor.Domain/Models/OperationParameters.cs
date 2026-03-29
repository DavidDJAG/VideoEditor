namespace VidEditor.Domain.Models;

public sealed record OperationParameters(
    string InputPath,
    string? OutputPath,
    TimeSpan? Start,
    TimeSpan? End,
    TimeSpan? SubtitleOffset,
    double SpeedFactor,
    IReadOnlyList<string> AdditionalInputs,
    IReadOnlyDictionary<string, string> Flags,
    EncodingProfile? EncodingProfile,
    IReadOnlyList<string>? ConcatInputs = null,
    ConvertOptions? ConvertOptions = null);
