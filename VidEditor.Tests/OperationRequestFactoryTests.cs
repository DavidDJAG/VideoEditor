using VidEditor.Application.Services;
using VidEditor.Domain.Models;

namespace VidEditor.Tests;

public sealed class OperationRequestFactoryTests
{
    private readonly OperationRequestFactory _factory = new();

    [Fact]
    public void Create_TrimKind_ReturnsTrimRequest()
    {
        var parameters = new OperationParameters(
            "in.mp4",
            "out.mp4",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3),
            null,
            1.0,
            [],
            new Dictionary<string, string>(),
            null);

        var request = _factory.Create(OperationKind.Trim, parameters);

        var typed = Assert.IsType<TrimRequest>(request);
        Assert.Equal(TimeSpan.FromSeconds(1), typed.Start);
        Assert.Equal(TimeSpan.FromSeconds(3), typed.End);
    }

    [Fact]
    public void Create_ExtractAudio_WithMp3OutputAndCopyCodec_UsesMp3Encoder()
    {
        var parameters = new OperationParameters(
            "in.mp4",
            "out.mp3",
            null,
            null,
            null,
            1.0,
            [],
            new Dictionary<string, string>
            {
                ["audioCodec"] = "copy"
            },
            null);

        var request = _factory.Create(OperationKind.ExtractAudio, parameters);

        var typed = Assert.IsType<ExtractAudioRequest>(request);
        Assert.Equal("libmp3lame", typed.AudioCodec);
    }

    [Fact]
    public void Create_ExtractVideo_WithVp9Alias_NormalizesCodecName()
    {
        var parameters = new OperationParameters(
            "in.mp4",
            "out.webm",
            null,
            null,
            null,
            1.0,
            [],
            new Dictionary<string, string>
            {
                ["videoCodec"] = "vp9"
            },
            null);

        var request = _factory.Create(OperationKind.ExtractVideo, parameters);

        var typed = Assert.IsType<ExtractVideoRequest>(request);
        Assert.Equal("libvpx-vp9", typed.VideoCodec);
    }

    [Fact]
    public void Create_ConcatKind_PopulatesManifestPath()
    {
        var parameters = new OperationParameters(
            "ignored.mp4",
            "joined.mp4",
            null,
            null,
            null,
            1.0,
            [],
            new Dictionary<string, string>(),
            null,
            ["a.mp4", "b.mp4"]);

        var request = _factory.Create(OperationKind.Concat, parameters);

        var typed = Assert.IsType<ConcatRequest>(request);
        Assert.Equal("joined.mp4.ffconcat", typed.ManifestPath);
    }


    [Fact]
    public void Create_ConvertKind_PrefersExplicitConvertOptions()
    {
        var options = new ConvertOptions(
            Container: "mkv",
            OverwriteMode: OverwriteMode.SkipExisting,
            FastStart: false,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Copy,
                Codec: "copy",
                RateControlMode: VideoRateControlMode.Bitrate,
                Crf: null,
                Bitrate: null,
                Preset: null,
                Tune: null,
                PixelFormat: null,
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.KeepSource,
                Width: null,
                Height: null,
                Profile: null,
                Level: null,
                GopSize: null),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Disable,
                Codec: "disabled",
                Bitrate: null,
                SampleRate: null,
                Channels: null,
                ChannelLayout: null));

        var parameters = new OperationParameters(
            "in.mov",
            "out.mkv",
            null,
            null,
            null,
            1.0,
            [],
            new Dictionary<string, string>(),
            new EncodingProfile("legacy", "libx264", "aac", "mp4", "2M", "128k", "yuv420p", "medium"),
            ConvertOptions: options);

        var request = _factory.Create(OperationKind.Convert, parameters);

        var typed = Assert.IsType<ConvertRequest>(request);
        Assert.Equal("mkv", typed.ConvertOptions.Container);
        Assert.Equal(StreamProcessingMode.Copy, typed.ConvertOptions.Video.Mode);
        Assert.Equal(StreamProcessingMode.Disable, typed.ConvertOptions.Audio.Mode);
    }

    [Fact]
    public void Catalog_V1Functional_ContainsCutJoinAndSplitAv()
    {
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.Trim).Phase);
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.Concat).Phase);
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.ExtractAudio).Phase);
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.ExtractVideo).Phase);
    }
}
