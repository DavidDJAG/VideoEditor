namespace VideoEdit.Domain.Models;

public sealed class MediaProbeResult
{
    public string InputPath { get; init; } = string.Empty;

    public string? ContainerFormat { get; init; }

    public string? Duration { get; init; }

    public string? BitRate { get; init; }

    public List<MediaStreamInfo> Streams { get; init; } = [];

    public string RawJson { get; init; } = string.Empty;

    public string SummaryText { get; init; } = string.Empty;
}
