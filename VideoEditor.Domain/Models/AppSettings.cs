namespace VideoEditor.Domain.Models;

public sealed record AppSettings(
    ToolPaths ToolPaths,
    ModuleFeatureFlags ModuleFlags,
    BetaExitCriteria BetaCriteria)
{
    public static AppSettings Default { get; } = new(
        new ToolPaths("ffmpeg", "ffprobe", "ffplay"),
        new ModuleFeatureFlags(),
        new BetaExitCriteria());
}

