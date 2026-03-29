using System.Text.Json.Serialization;

namespace VideoEditor.Domain.Models;

public sealed record AppSettings
{
    [JsonConstructor]
    public AppSettings(
        ToolPaths toolPaths,
        ModuleFeatureFlags moduleFlags,
        BetaExitCriteria betaCriteria,
        ConvertPresetRecord[]? convertPresets = null)
    {
        ToolPaths = toolPaths;
        ModuleFlags = moduleFlags;
        BetaCriteria = betaCriteria;
        ConvertPresets = convertPresets ?? [];
    }

    public ToolPaths ToolPaths { get; init; }

    public ModuleFeatureFlags ModuleFlags { get; init; }

    public BetaExitCriteria BetaCriteria { get; init; }

    public ConvertPresetRecord[] ConvertPresets { get; init; }

    public static AppSettings Default { get; } = new(
        new ToolPaths("ffmpeg", "ffprobe", "ffplay"),
        new ModuleFeatureFlags(),
        new BetaExitCriteria(),
        []);
}
