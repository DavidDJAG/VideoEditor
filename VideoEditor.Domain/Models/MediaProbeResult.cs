namespace VideoEditor.Domain.Models;

public sealed record MediaProbeResult(
    string FilePath,
    TimeSpan Duration,
    long SizeBytes,
    string Container,
    int VideoStreamCount,
    int AudioStreamCount,
    int SubtitleStreamCount,
    int? Width,
    int? Height,
    double? FrameRate,
    int? AudioSampleRate,
    int? AudioChannels,
    string RawJson);
