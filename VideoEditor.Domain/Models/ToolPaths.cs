namespace VideoEditor.Domain.Models;

public sealed record ToolPaths(
    string FfmpegPath,
    string FfprobePath,
    string? FfplayPath);
