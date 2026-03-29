namespace VidEditor.Domain.Models;

public sealed record ContainerDefinition(
    string Id,
    string DisplayName,
    string FfmpegMuxerName,
    string DefaultExtension,
    IReadOnlyList<string> AlternateExtensions,
    bool SupportsVideo,
    bool SupportsAudio,
    bool SupportsSubtitles,
    bool IsUserSelectable,
    int SortOrder)
{
    public IReadOnlyList<string> AllExtensions => _allExtensions ??= BuildAllExtensions();

    public bool SupportsFastStart => Id is "mp4" or "mov" or "m4v";

    public bool IsAudioOnly => !SupportsVideo && SupportsAudio;

    public bool MatchesExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var normalized = NormalizeExtension(extension);
        return AllExtensions.Any(value => value.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString() => DisplayName;

    private IReadOnlyList<string>? _allExtensions;

    private IReadOnlyList<string> BuildAllExtensions()
    {
        var values = new List<string>();
        Add(DefaultExtension, values);

        foreach (var extension in AlternateExtensions)
        {
            Add(extension, values);
        }

        return values;

        static void Add(string? extension, ICollection<string> values)
        {
            var normalized = NormalizeExtension(extension);
            if (!string.IsNullOrWhiteSpace(normalized) && !values.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(normalized);
            }
        }
    }

    private static string NormalizeExtension(string? extension)
        => string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.Trim().TrimStart('.').ToLowerInvariant();
}
