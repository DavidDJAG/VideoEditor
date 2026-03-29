namespace VidEditor.Domain.Models;

public sealed record MediaStreamInfo(
    int SourceIndex,
    string StreamType,
    int TypeIndex,
    string? CodecName,
    string? CodecLongName,
    string? Language,
    string? Title,
    int? Width,
    int? Height,
    double? FrameRate,
    int? SampleRate,
    int? Channels,
    string? ChannelLayout,
    bool IsDefault,
    bool IsForced);

public sealed record MediaProbeResult(
    string FilePath,
    TimeSpan Duration,
    long SizeBytes,
    string Container,
    string? VideoCodec,
    string? AudioCodec,
    int VideoStreamCount,
    int AudioStreamCount,
    int SubtitleStreamCount,
    int? Width,
    int? Height,
    double? FrameRate,
    int? AudioSampleRate,
    int? AudioChannels,
    string? AudioChannelLayout,
    string? AudioSampleFormat,
    string RawJson,
    IReadOnlyList<MediaStreamInfo>? Streams = null,
    IReadOnlyDictionary<string, string>? FormatTags = null)
{
    public IReadOnlyList<MediaStreamInfo> StreamInfos { get; init; } = Streams ?? Array.Empty<MediaStreamInfo>();
    public IReadOnlyDictionary<string, string> Tags { get; init; } = FormatTags ?? new Dictionary<string, string>();
}
