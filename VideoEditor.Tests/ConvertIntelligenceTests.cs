using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class ConvertIntelligenceTests
{
    [Fact]
    public void BuildAdaptivePresetCatalog_BalancedPreset_UsesDetectedH264Mp4Stack()
    {
        var snapshot = CreateSnapshot(
            videoEncoders: ["h264_nvenc", "libx265"],
            audioEncoders: ["aac", "libopus"],
            muxers: ["mp4", "mkv"],
            hwaccels: ["cuda"]);

        var catalog = ConvertIntelligence.BuildAdaptivePresetCatalog(snapshot, probe: null, preferredContainer: "mp4");
        var preset = catalog.BuiltInPresets["Balanced H.264 MP4"];

        Assert.Equal("mp4", preset.Container);
        Assert.Equal("h264_nvenc", preset.Video.Codec);
        Assert.Equal(VideoRateControlMode.Bitrate, preset.Video.RateControlMode);
        Assert.Equal("2500k", preset.Video.Bitrate);
        Assert.True(preset.UseHardwareAcceleration);
        Assert.Equal("aac", preset.Audio.Codec);
        Assert.True(preset.FastStart);
    }

    [Fact]
    public void BuildAdaptivePresetCatalog_EfficientPreset_FallsBackToAv1MkvWhenHevcMissing()
    {
        var snapshot = CreateSnapshot(
            videoEncoders: ["libsvtav1", "libx264"],
            audioEncoders: ["libopus", "aac"],
            muxers: ["mkv", "webm"],
            hwaccels: []);

        var catalog = ConvertIntelligence.BuildAdaptivePresetCatalog(snapshot, probe: null, preferredContainer: "mp4");
        var preset = catalog.BuiltInPresets["Efficient H.265 MP4"];

        Assert.Equal("mkv", preset.Container);
        Assert.Equal("libsvtav1", preset.Video.Codec);
        Assert.Equal(VideoRateControlMode.ConstantQuality, preset.Video.RateControlMode);
        Assert.Equal(30, preset.Video.Crf);
        Assert.Equal("libopus", preset.Audio.Codec);
    }

    [Fact]
    public void BuildAdaptivePresetCatalog_StreamCopyPreset_FollowsDetectedAudioOnlySource()
    {
        var snapshot = CreateSnapshot(
            videoEncoders: ["libx264"],
            audioEncoders: ["aac"],
            muxers: ["m4a", "mp4", "mkv"],
            hwaccels: []);
        var probe = new MediaProbeResult(
            FilePath: "input.wav",
            Duration: TimeSpan.FromMinutes(3),
            SizeBytes: 1024,
            Container: "wav",
            VideoCodec: null,
            AudioCodec: "pcm_s16le",
            VideoStreamCount: 0,
            AudioStreamCount: 1,
            SubtitleStreamCount: 0,
            Width: null,
            Height: null,
            FrameRate: null,
            AudioSampleRate: 48000,
            AudioChannels: 2,
            AudioChannelLayout: "stereo",
            AudioSampleFormat: "s16",
            RawJson: "{}");

        var catalog = ConvertIntelligence.BuildAdaptivePresetCatalog(snapshot, probe, preferredContainer: "m4a");
        var preset = catalog.BuiltInPresets["Stream Copy / Remux"];

        Assert.Equal("m4a", preset.Container);
        Assert.Equal(StreamProcessingMode.Disable, preset.Video.Mode);
        Assert.Equal(StreamProcessingMode.Copy, preset.Audio.Mode);
    }


    [Fact]
    public void BuildAdaptivePresetCatalog_ReferenceAv1Preset_MatchesRequestedCommandProfile()
    {
        var catalog = ConvertIntelligence.BuildAdaptivePresetCatalog(capabilities: null, probe: null, preferredContainer: null);
        var preset = catalog.BuiltInPresets["AV1 1440p 10-bit MKV"];

        Assert.Equal("mkv", preset.Container);
        Assert.False(preset.FastStart);
        Assert.Equal(StreamProcessingMode.Encode, preset.Video.Mode);
        Assert.Equal("libsvtav1", preset.Video.Codec);
        Assert.Equal(VideoRateControlMode.ConstantQuality, preset.Video.RateControlMode);
        Assert.Equal(28, preset.Video.Crf);
        Assert.Equal("6", preset.Video.Preset);
        Assert.Equal("yuv420p10le", preset.Video.PixelFormat);
        Assert.Equal(StreamProcessingMode.Encode, preset.Audio.Mode);
        Assert.Equal("libopus", preset.Audio.Codec);
        Assert.Equal("128k", preset.Audio.Bitrate);
    }

    [Fact]
    public void BuildAdvancedCompatibilityAdvisories_WarnsForOpusInMp4AndOddHevcScaling()
    {
        var snapshot = CreateSnapshot(
            videoEncoders: ["libx265"],
            audioEncoders: ["libopus"],
            muxers: ["mp4", "mkv"],
            hwaccels: []);
        var options = new ConvertOptions(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libx265",
                RateControlMode: VideoRateControlMode.ConstantQuality,
                Crf: 24,
                Bitrate: null,
                Preset: "medium",
                Tune: null,
                PixelFormat: "yuv444p",
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.SetOutput,
                Width: 1279,
                Height: 719,
                Profile: null,
                Level: null,
                GopSize: null),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libopus",
                Bitrate: "128k",
                SampleRate: null,
                Channels: null,
                ChannelLayout: null));

        var advisories = ConvertIntelligence.BuildAdvancedCompatibilityAdvisories(options, snapshot, probe: null, outputPath: "out.mp4");

        Assert.Contains(advisories, message => message.Contains("Opus inside '.mp4'", StringComparison.Ordinal));
        Assert.Contains(advisories, message => message.Contains("Pixel format 'yuv444p'", StringComparison.Ordinal));
        Assert.Contains(advisories, message => message.Contains("Width 1279 is odd", StringComparison.Ordinal));
        Assert.Contains(advisories, message => message.Contains("Height 719 is odd", StringComparison.Ordinal));
    }

    private static ToolchainCapabilitiesSnapshot CreateSnapshot(
        IReadOnlyList<string> videoEncoders,
        IReadOnlyList<string> audioEncoders,
        IReadOnlyList<string> muxers,
        IReadOnlyList<string> hwaccels)
        => new(
            CapturedAt: DateTimeOffset.UtcNow,
            Ffmpeg: new ToolchainBinaryDiagnostic("ffmpeg", "ffmpeg", "C:/ffmpeg/bin/ffmpeg.exe", false, true, false, null),
            Ffprobe: new ToolchainBinaryDiagnostic("ffprobe", "ffprobe", "C:/ffmpeg/bin/ffprobe.exe", false, true, false, null),
            Ffplay: new ToolchainBinaryDiagnostic("ffplay", "ffplay", "C:/ffmpeg/bin/ffplay.exe", false, true, false, null),
            FfmpegVersion: "ffmpeg test",
            SupportedVideoCodecs: [],
            HardwareAccelerationMethods: hwaccels,
            VideoEncoders: videoEncoders,
            AudioEncoders: audioEncoders,
            Muxers: muxers,
            PixelFormats: ["yuv420p", "yuv444p"]);
}
