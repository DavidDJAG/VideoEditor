namespace VidEditor.Domain.Models;

public static class ContainerCatalog
{
    private static readonly IReadOnlyList<ContainerDefinition> Definitions =
    [
        new("mp4", "MP4", "mp4", ".mp4", [".m4v"], SupportsVideo: true, SupportsAudio: true, SupportsSubtitles: true, IsUserSelectable: true, SortOrder: 10),
        new("mkv", "MKV", "matroska", ".mkv", [], SupportsVideo: true, SupportsAudio: true, SupportsSubtitles: true, IsUserSelectable: true, SortOrder: 20),
        new("webm", "WebM", "webm", ".webm", [], SupportsVideo: true, SupportsAudio: true, SupportsSubtitles: true, IsUserSelectable: true, SortOrder: 30),
        new("mov", "MOV", "mov", ".mov", [], SupportsVideo: true, SupportsAudio: true, SupportsSubtitles: true, IsUserSelectable: true, SortOrder: 40),
        new("avi", "AVI", "avi", ".avi", [], SupportsVideo: true, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: true, SortOrder: 50),
        new("m4a", "M4A", "ipod", ".m4a", [], SupportsVideo: false, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: true, SortOrder: 60),
        new("mp3", "MP3", "mp3", ".mp3", [], SupportsVideo: false, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: true, SortOrder: 70),
        new("flac", "FLAC", "flac", ".flac", [], SupportsVideo: false, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: true, SortOrder: 80),
        new("ogg", "OGG", "ogg", ".ogg", [".oga"], SupportsVideo: false, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: true, SortOrder: 90),
        new("wav", "WAV", "wav", ".wav", [], SupportsVideo: false, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: true, SortOrder: 100),
        new("aac", "AAC (ADTS)", "adts", ".aac", [], SupportsVideo: false, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: false, SortOrder: 110),
        new("opus", "Opus", "opus", ".opus", [], SupportsVideo: false, SupportsAudio: true, SupportsSubtitles: false, IsUserSelectable: false, SortOrder: 120)
    ];

    private static readonly IReadOnlyDictionary<string, ContainerDefinition> ById = Definitions.ToDictionary(static definition => definition.Id, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> AliasToId = BuildAliasMap();

    public static IReadOnlyList<ContainerDefinition> GetUserSelectableContainers()
        => Definitions.Where(static definition => definition.IsUserSelectable)
            .OrderBy(static definition => definition.SortOrder)
            .ToArray();

    public static IReadOnlyList<ContainerDefinition> GetAvailableUserSelectableContainers(IReadOnlyCollection<string>? availableMuxers)
        => GetUserSelectableContainers()
            .Where(definition => IsAvailable(definition.Id, availableMuxers))
            .ToArray();

    public static string NormalizeId(string? value)
        => TryResolve(value, out var definition)
            ? definition.Id
            : string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().TrimStart('.').ToLowerInvariant();

    public static bool IsKnown(string? value)
        => TryResolve(value, out _);

    public static bool IsUserSelectable(string? value)
        => TryResolve(value, out var definition) && definition.IsUserSelectable;

    public static bool TryResolve(string? value, out ContainerDefinition definition)
    {
        definition = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (ById.TryGetValue(normalized, out definition))
        {
            return true;
        }

        normalized = normalized.TrimStart('.').ToLowerInvariant();
        if (ById.TryGetValue(normalized, out definition))
        {
            return true;
        }

        if (AliasToId.TryGetValue(normalized, out var id) && ById.TryGetValue(id, out definition))
        {
            return true;
        }

        return false;
    }

    public static ContainerDefinition ResolveOrDefault(string? value, string defaultId = "mp4")
        => TryResolve(value, out var definition)
            ? definition
            : ById[NormalizeId(defaultId)];

    public static string ResolveMuxerName(string? value, string defaultId = "mp4")
        => ResolveOrDefault(value, defaultId).FfmpegMuxerName;

    public static string ResolveDefaultExtension(string? value, string defaultId = "mp4")
        => ResolveOrDefault(value, defaultId).DefaultExtension;

    public static string ResolveDisplayName(string? value, string defaultName = "Unknown")
        => TryResolve(value, out var definition)
            ? definition.DisplayName
            : defaultName;

    public static bool MatchesExtension(string? containerId, string? extension)
        => TryResolve(containerId, out var definition) && definition.MatchesExtension(extension);

    public static bool IsAvailable(string? containerId, IReadOnlyCollection<string>? availableMuxers)
    {
        if (!TryResolve(containerId, out var definition))
        {
            return false;
        }

        return availableMuxers is not { Count: > 0 }
               || availableMuxers.Any(muxer => muxer.Equals(definition.FfmpegMuxerName, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeSourceContainer(string? probeContainer)
    {
        if (string.IsNullOrWhiteSpace(probeContainer))
        {
            return string.Empty;
        }

        if (TryResolve(probeContainer, out var exactMatch))
        {
            return exactMatch.Id;
        }

        foreach (var token in probeContainer.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryResolve(token, out var definition))
            {
                return definition.Id;
            }
        }

        return NormalizeId(probeContainer);
    }

    private static IReadOnlyDictionary<string, string> BuildAliasMap()
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in Definitions)
        {
            Add(definition.Id, definition.Id);
            Add(definition.FfmpegMuxerName, definition.Id);
            Add(definition.DefaultExtension.TrimStart('.'), definition.Id);

            foreach (var extension in definition.AlternateExtensions)
            {
                Add(extension.TrimStart('.'), definition.Id);
            }
        }

        Add("matroska", "mkv");
        Add("mov,mp4,m4a,3gp,3g2,mj2", "mp4");
        Add("ipod", "m4a");
        Add("adts", "aac");
        Add("oga", "ogg");

        return aliases;

        void Add(string? alias, string id)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                aliases[alias.Trim().TrimStart('.').ToLowerInvariant()] = id;
            }
        }
    }
}
