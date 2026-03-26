namespace VideoEdit.Domain.Models;

public sealed class FfmpegProgressInfo
{
    public TimeSpan? ProcessedTime { get; init; }

    public string? FramesPerSecond { get; init; }

    public string? Speed { get; init; }

    public double? Percentage { get; init; }

    public string RawLine { get; init; } = string.Empty;
}
