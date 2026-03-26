namespace VideoEdit.Domain.Models;

public sealed class EncodingPreset
{
    public required string Name { get; init; }

    public required MediaJobType JobType { get; init; }

    public required string ContainerExtension { get; init; }

    public string? VideoCodec { get; init; }

    public string? AudioCodec { get; init; }

    public string? Crf { get; init; }

    public string? Preset { get; init; }

    public string? AudioBitrate { get; init; }

    public string? PixelFormat { get; init; }

    public bool AudioCopy { get; init; }

    public bool VideoCopy { get; init; }

    public override string ToString() => Name;

    public static IReadOnlyList<EncodingPreset> CreateDefaults() =>
    [
        new()
        {
            Name = "M4A copy",
            JobType = MediaJobType.ExtractAudio,
            ContainerExtension = "m4a",
            AudioCodec = "copy",
            AudioCopy = true
        },
        new()
        {
            Name = "MP3 calidad alta",
            JobType = MediaJobType.ExtractAudio,
            ContainerExtension = "mp3",
            AudioCodec = "libmp3lame",
            AudioBitrate = "q=2"
        },
        new()
        {
            Name = "WAV PCM",
            JobType = MediaJobType.ExtractAudio,
            ContainerExtension = "wav",
            AudioCodec = "pcm_s16le"
        },
        new()
        {
            Name = "H.264 + AAC compatible",
            JobType = MediaJobType.Convert,
            ContainerExtension = "mp4",
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Crf = "23",
            Preset = "medium",
            AudioBitrate = "192k",
            PixelFormat = "yuv420p"
        },
        new()
        {
            Name = "H.265/HEVC + AAC",
            JobType = MediaJobType.Convert,
            ContainerExtension = "mp4",
            VideoCodec = "libx265",
            AudioCodec = "aac",
            Crf = "28",
            Preset = "medium",
            AudioBitrate = "192k",
            PixelFormat = "yuv420p"
        },
        new()
        {
            Name = "AV1 + Opus alto desempeño",
            JobType = MediaJobType.Convert,
            ContainerExtension = "mkv",
            VideoCodec = "libsvtav1",
            AudioCodec = "libopus",
            Crf = "28",
            Preset = "6",
            AudioBitrate = "128k",
            PixelFormat = "yuv420p10le"
        },
        new()
        {
            Name = "Solo audio a MP3",
            JobType = MediaJobType.Convert,
            ContainerExtension = "mp3",
            AudioCodec = "libmp3lame",
            AudioBitrate = "192k"
        }
    ];
}
