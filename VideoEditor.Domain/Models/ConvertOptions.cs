namespace VideoEditor.Domain.Models;

public enum StreamProcessingMode
{
    Encode,
    Copy,
    Disable
}

public enum VideoRateControlMode
{
    ConstantQuality,
    Bitrate
}

public enum FrameRateMode
{
    KeepSource,
    SetOutput
}

public enum ScaleMode
{
    KeepSource,
    SetOutput
}

public enum OverwriteMode
{
    Overwrite,
    SkipExisting
}

public enum VideoPassMode
{
    SinglePass,
    TwoPass
}

public enum VideoDeinterlaceMode
{
    Off,
    Yadif
}

public enum AudioNormalizationMode
{
    None,
    Loudnorm,
    Dynaudnorm
}

public enum SubtitleProcessingMode
{
    Disable,
    Copy,
    Encode,
    BurnIn
}

public sealed record SubtitleOptions(
    SubtitleProcessingMode Mode,
    int? SourceStreamIndex = null,
    string? Language = null,
    bool SetAsDefault = false,
    IReadOnlyList<int>? AdditionalSourceStreamIndexes = null,
    string? Codec = null)
{
    public static SubtitleOptions Disabled()
        => new(SubtitleProcessingMode.Disable);

    public IReadOnlyList<int> GetSelectedStreamIndexes()
        => NormalizeStreamIndexCollection(SourceStreamIndex, AdditionalSourceStreamIndexes);

    internal static IReadOnlyList<int> NormalizeStreamIndexCollection(int? primaryIndex, IReadOnlyList<int>? additionalIndexes)
    {
        var values = new List<int>();
        if (primaryIndex is >= 0)
        {
            values.Add(primaryIndex.Value);
        }

        if (additionalIndexes is not null)
        {
            foreach (var index in additionalIndexes.Where(static value => value >= 0))
            {
                if (!values.Contains(index))
                {
                    values.Add(index);
                }
            }
        }

        return values;
    }
}

public sealed record MetadataOptions(
    bool CopyMetadata,
    bool CopyChapters,
    string? Title = null,
    string? Artist = null,
    string? Comment = null)
{
    public static MetadataOptions CreateDefault()
        => new(CopyMetadata: true, CopyChapters: true);
}

public sealed record VideoEncodingOptions(
    StreamProcessingMode Mode,
    string Codec,
    VideoRateControlMode RateControlMode,
    int? Crf,
    string? Bitrate,
    string? Preset,
    string? Tune,
    string? PixelFormat,
    FrameRateMode FrameRateMode,
    double? FrameRate,
    ScaleMode ScaleMode,
    int? Width,
    int? Height,
    string? Profile,
    string? Level,
    int? GopSize,
    int? SourceStreamIndex = null,
    VideoPassMode PassMode = VideoPassMode.SinglePass,
    VideoDeinterlaceMode DeinterlaceMode = VideoDeinterlaceMode.Off,
    int? CropX = null,
    int? CropY = null,
    int? CropWidth = null,
    int? CropHeight = null,
    bool PadToSize = false,
    int? PadWidth = null,
    int? PadHeight = null,
    int? PadX = null,
    int? PadY = null)
{
    public static VideoEncodingOptions CreateBalancedH264()
        => new(
            Mode: StreamProcessingMode.Encode,
            Codec: "libx264",
            RateControlMode: VideoRateControlMode.Bitrate,
            Crf: null,
            Bitrate: "2500k",
            Preset: "medium",
            Tune: null,
            PixelFormat: "yuv420p",
            FrameRateMode: FrameRateMode.KeepSource,
            FrameRate: null,
            ScaleMode: ScaleMode.KeepSource,
            Width: null,
            Height: null,
            Profile: null,
            Level: null,
            GopSize: null,
            PassMode: VideoPassMode.SinglePass,
            DeinterlaceMode: VideoDeinterlaceMode.Off);
}

public sealed record AudioEncodingOptions(
    StreamProcessingMode Mode,
    string Codec,
    string? Bitrate,
    int? SampleRate,
    int? Channels,
    string? ChannelLayout,
    int? SourceStreamIndex = null,
    IReadOnlyList<int>? AdditionalSourceStreamIndexes = null,
    AudioNormalizationMode NormalizationMode = AudioNormalizationMode.None,
    double? LoudnessTarget = null,
    double? TruePeak = null,
    double? LoudnessRange = null)
{
    public static AudioEncodingOptions CreateBalancedAac()
        => new(
            Mode: StreamProcessingMode.Encode,
            Codec: "aac",
            Bitrate: "160k",
            SampleRate: null,
            Channels: null,
            ChannelLayout: null,
            NormalizationMode: AudioNormalizationMode.None,
            LoudnessTarget: -16,
            TruePeak: -1.5,
            LoudnessRange: 11);

    public IReadOnlyList<int> GetSelectedStreamIndexes()
        => SubtitleOptions.NormalizeStreamIndexCollection(SourceStreamIndex, AdditionalSourceStreamIndexes);
}

public sealed record ConvertOptions(
    string Container,
    OverwriteMode OverwriteMode,
    bool FastStart,
    bool UseHardwareAcceleration,
    VideoEncodingOptions Video,
    AudioEncodingOptions Audio,
    SubtitleOptions? Subtitle = null,
    MetadataOptions? Metadata = null)
{
    public static ConvertOptions CreateBalancedMp4H264()
        => new(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
            UseHardwareAcceleration: false,
            Video: VideoEncodingOptions.CreateBalancedH264(),
            Audio: AudioEncodingOptions.CreateBalancedAac(),
            Subtitle: SubtitleOptions.Disabled(),
            Metadata: MetadataOptions.CreateDefault());

    public static ConvertOptions FromLegacyProfile(EncodingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var normalizedContainer = NormalizeContainer(profile.Container);
        var videoMode = ResolveStreamMode(profile.VideoCodec);
        var audioMode = ResolveStreamMode(profile.AudioCodec);

        return new ConvertOptions(
            Container: normalizedContainer,
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: SupportsFastStart(normalizedContainer),
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: videoMode,
                Codec: NormalizeToken(profile.VideoCodec),
                RateControlMode: VideoRateControlMode.Bitrate,
                Crf: null,
                Bitrate: NormalizeOptionalToken(profile.VideoBitrate),
                Preset: NormalizeOptionalToken(profile.Preset),
                Tune: null,
                PixelFormat: NormalizeOptionalToken(profile.PixelFormat),
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.KeepSource,
                Width: null,
                Height: null,
                Profile: null,
                Level: null,
                GopSize: null,
                SourceStreamIndex: null,
                PassMode: VideoPassMode.SinglePass,
                DeinterlaceMode: VideoDeinterlaceMode.Off),
            Audio: new AudioEncodingOptions(
                Mode: audioMode,
                Codec: NormalizeToken(profile.AudioCodec),
                Bitrate: NormalizeOptionalToken(profile.AudioBitrate),
                SampleRate: null,
                Channels: null,
                ChannelLayout: null,
                SourceStreamIndex: null,
                AdditionalSourceStreamIndexes: null,
                NormalizationMode: AudioNormalizationMode.None,
                LoudnessTarget: -16,
                TruePeak: -1.5,
                LoudnessRange: 11),
            Subtitle: SubtitleOptions.Disabled(),
            Metadata: MetadataOptions.CreateDefault());
    }

    public ConvertOptions Normalize()
        => this with
        {
            Container = NormalizeContainer(Container),
            Video = NormalizeVideo(Video),
            Audio = NormalizeAudio(Audio),
            Subtitle = NormalizeSubtitle(Subtitle),
            Metadata = NormalizeMetadata(Metadata)
        };

    private static VideoEncodingOptions NormalizeVideo(VideoEncodingOptions video)
        => video with
        {
            Codec = NormalizeToken(video.Codec),
            Bitrate = NormalizeOptionalToken(video.Bitrate),
            Preset = NormalizeOptionalToken(video.Preset),
            Tune = NormalizeOptionalToken(video.Tune),
            PixelFormat = NormalizeOptionalToken(video.PixelFormat),
            Profile = NormalizeOptionalToken(video.Profile),
            Level = NormalizeOptionalToken(video.Level)
        };

    private static AudioEncodingOptions NormalizeAudio(AudioEncodingOptions audio)
        => audio with
        {
            Codec = NormalizeToken(audio.Codec),
            Bitrate = NormalizeOptionalToken(audio.Bitrate),
            ChannelLayout = NormalizeOptionalToken(audio.ChannelLayout),
            AdditionalSourceStreamIndexes = SubtitleOptions.NormalizeStreamIndexCollection(null, audio.AdditionalSourceStreamIndexes)
        };

    private static SubtitleOptions NormalizeSubtitle(SubtitleOptions? subtitle)
        => subtitle is null
            ? SubtitleOptions.Disabled()
            : subtitle with
            {
                Language = NormalizeOptionalToken(subtitle.Language),
                Codec = NormalizeOptionalToken(subtitle.Codec),
                AdditionalSourceStreamIndexes = SubtitleOptions.NormalizeStreamIndexCollection(null, subtitle.AdditionalSourceStreamIndexes)
            };

    private static MetadataOptions NormalizeMetadata(MetadataOptions? metadata)
        => metadata is null
            ? MetadataOptions.CreateDefault()
            : metadata with
            {
                Title = NormalizeOptionalToken(metadata.Title),
                Artist = NormalizeOptionalToken(metadata.Artist),
                Comment = NormalizeOptionalToken(metadata.Comment)
            };

    private static StreamProcessingMode ResolveStreamMode(string codec)
        => NormalizeToken(codec).ToLowerInvariant() switch
        {
            "copy" => StreamProcessingMode.Copy,
            "disable" or "disabled" or "none" => StreamProcessingMode.Disable,
            _ => StreamProcessingMode.Encode
        };

    private static string NormalizeContainer(string container)
        => string.IsNullOrWhiteSpace(container)
            ? string.Empty
            : container.Trim().TrimStart('.').ToLowerInvariant();

    private static string NormalizeToken(string value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();

    private static string? NormalizeOptionalToken(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static bool SupportsFastStart(string container)
        => container is "mp4" or "mov" or "m4v";
}
