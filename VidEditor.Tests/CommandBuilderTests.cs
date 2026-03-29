using VidEditor.Application.Services;
using VidEditor.Domain.Models;

namespace VidEditor.Tests;

public sealed class CommandBuilderTests
{
    private readonly CommandBuilder _builder = new();
    private static readonly EncodingProfile DefaultProfile = new("default", "libx264", "aac", "mp4", "2M", "128k", "yuv420p", "medium");

    [Fact]
    public void BuildTrim_UsesTypedRequestAndExpectedFlags()
    {
        var command = _builder.Build(new TrimRequest("in.mp4", "out.mp4", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(8)));

        Assert.Contains("-ss 00:00:03", command);
        Assert.Contains("-t 00:00:05", command);
        Assert.Contains("\"in.mp4\"", command);
        Assert.Contains("\"out.mp4\"", command);
    }

    [Fact]
    public void BuildSubtitleBurnIn_UsesSubtitlesFilter()
    {
        var command = _builder.Build(new SubtitleRequest("in.mp4", "captions.srt", "out.mp4", SubtitleMode.BurnIn));

        Assert.Contains("-vf", command);
        Assert.Contains("subtitles=", command);
    }

    [Fact]
    public void BuildWatermarkText_UsesDrawText()
    {
        var command = _builder.Build(new WatermarkOverlayRequest("in.mp4", "out.mp4", null, "CONFIDENTIAL", "20:30"));

        Assert.Contains("drawtext", command);
        Assert.Contains("-c:a copy", command);
    }

    [Fact]
    public void BuildSegmentHls_UsesHlsPackagingFlags()
    {
        var command = _builder.Build(new SegmentHlsRequest("in.mp4", "master.m3u8", "segment_%03d.ts", 4));

        Assert.Contains("-f hls", command);
        Assert.Contains("-hls_time 4", command);
        Assert.Contains("\"master.m3u8\"", command);
    }

    [Fact]
    public void BuildExtractAudio_UsesAudioOnlyFlagsAndDeterministicOrder()
    {
        var command = _builder.Build(new ExtractAudioRequest("in.mp4", "out.m4a"));

        Assert.Equal("-y -i \"in.mp4\" -vn -c:a copy \"out.m4a\"", command);
    }

    [Fact]
    public void BuildExtractVideo_UsesVideoOnlyFlagsAndDeterministicOrder()
    {
        var command = _builder.Build(new ExtractVideoRequest("in.mp4", "out.mp4"));

        Assert.Equal("-y -i \"in.mp4\" -an -c:v copy \"out.mp4\"", command);
    }

    [Fact]
    public void BuildConcat_UsesConcatProtocolAndDeterministicOrder()
    {
        var command = _builder.Build(new ConcatRequest(["a.mp4", "b.mp4"], "joined.mp4", "joined.mp4.ffconcat"));

        Assert.Equal("-y -f concat -safe 0 -i \"joined.mp4.ffconcat\" -c copy \"joined.mp4\"", command);
    }

    [Fact]
    public void BuildConcat_FromOperationParameters_UsesOrderedConcatInputs()
    {
        var command = _builder.BuildConcat(new OperationParameters(
            InputPath: "ignored-by-concat",
            OutputPath: "joined.mp4",
            Start: null,
            End: null,
            SubtitleOffset: null,
            SpeedFactor: 1.0,
            AdditionalInputs: [],
            Flags: new Dictionary<string, string>(),
            EncodingProfile: DefaultProfile,
            ConcatInputs: ["seg1.mp4", "seg2.mp4", "seg3.mp4"]));

        Assert.Equal("-y -f concat -safe 0 -i \"joined.mp4.ffconcat\" -c copy \"joined.mp4\"", command);
    }

    [Fact]
    public void BuildConvert_FromLegacyProfile_KeepsBackwardCompatibleFlags()
    {
        var command = _builder.Build(new ConvertRequest("in.mov", "out.mp4", DefaultProfile));

        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-b:v 2M", command);
        Assert.Contains("-preset medium", command);
        Assert.Contains("-pix_fmt yuv420p", command);
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-b:a 128k", command);
        Assert.Contains("-movflags +faststart", command);
        Assert.Contains("-f mp4", command);
    }

    [Fact]
    public void BuildConvert_WithAdvancedVideoAndAudioSettings_UsesConditionalArguments()
    {
        var options = new ConvertOptions(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
            UseHardwareAcceleration: true,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libx265",
                RateControlMode: VideoRateControlMode.ConstantQuality,
                Crf: 24,
                Bitrate: null,
                Preset: "slow",
                Tune: "grain",
                PixelFormat: "yuv420p10le",
                FrameRateMode: FrameRateMode.SetOutput,
                FrameRate: 23.976,
                ScaleMode: ScaleMode.SetOutput,
                Width: 1280,
                Height: 720,
                Profile: "main10",
                Level: "5.1",
                GopSize: 48),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "aac",
                Bitrate: "192k",
                SampleRate: 48000,
                Channels: 2,
                ChannelLayout: "stereo"));

        var command = _builder.Build(new ConvertRequest("in.mkv", "out.mp4", options));

        Assert.Contains("-hwaccel auto", command);
        Assert.Contains("-c:v libx265", command);
        Assert.Contains("-crf 24", command);
        Assert.Contains("-preset slow", command);
        Assert.Contains("-tune grain", command);
        Assert.Contains("-pix_fmt yuv420p10le", command);
        Assert.Contains("-vf \"scale=1280:720\"", command);
        Assert.Contains("-r 23.976", command);
        Assert.Contains("-profile:v main10", command);
        Assert.Contains("-level:v 5.1", command);
        Assert.Contains("-g 48", command);
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-b:a 192k", command);
        Assert.Contains("-ar 48000", command);
        Assert.Contains("-ac 2", command);
        Assert.Contains("-channel_layout stereo", command);
        Assert.Contains("-movflags +faststart", command);
        Assert.Contains("-f mp4", command);
    }

    [Fact]
    public void BuildConvert_WithCopyVideoAndDisabledAudio_UsesStreamDirectives()
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

        var command = _builder.Build(new ConvertRequest("in.mov", "out.mkv", options));

        Assert.StartsWith("-n -i \"in.mov\"", command);
        Assert.Contains("-c:v copy", command);
        Assert.Contains("-an", command);
        Assert.Contains("-f matroska", command);
        Assert.DoesNotContain("-movflags +faststart", command);
    }

    [Fact]
    public void InvalidConvertRequest_WithBothStreamsDisabled_ThrowsWithValidationErrors()
    {
        var options = new ConvertOptions(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Disable,
                Codec: "disabled",
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

        var ex = Assert.Throws<InvalidOperationException>(() => _builder.Build(new ConvertRequest("in.mp4", "out.mp4", options)));
        Assert.Contains("At least one stream must remain enabled", ex.Message);
    }

    [Fact]
    public void InvalidRequest_ThrowsWithValidationErrors()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _builder.Build(new NormalizeLoudnessRequest("", "", 1)));
        Assert.Contains("Invalid operation request", ex.Message);
    }

    [Fact]
    public void InvalidConvertRequest_WithTechnicalMuxerIdentifier_ThrowsWithValidationErrors()
    {
        var options = ConvertOptions.CreateBalancedMp4H264() with { Container = "mkvtimestamp_v2" };

        var ex = Assert.Throws<InvalidOperationException>(() => _builder.Build(new ConvertRequest("in.mp4", "out.mkv", options)));

        Assert.Contains("Container 'mkvtimestamp_v2' is not supported", ex.Message);
    }


    [Fact]
    public void BuildConvert_TwoPassBitrateEncoding_ReturnsTwoCommands()
    {
        var options = new ConvertOptions(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libx264",
                RateControlMode: VideoRateControlMode.Bitrate,
                Crf: null,
                Bitrate: "3500k",
                Preset: "slow",
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
                PassMode: VideoPassMode.TwoPass),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "aac",
                Bitrate: "160k",
                SampleRate: null,
                Channels: null,
                ChannelLayout: null));

        var commands = _builder.BuildCommandSequence(new ConvertRequest("in.mov", "out.mp4", options));

        Assert.Equal(2, commands.Count);
        Assert.Contains("-pass 1", commands[0]);
        Assert.Contains("-an -f null", commands[0]);
        Assert.Contains("-pass 2", commands[1]);
        Assert.Contains("\"out.mp4\"", commands[1]);
        Assert.Contains("-c:a aac", commands[1]);
    }

    [Fact]
    public void BuildConvert_WithVideoFiltersAndAudioNormalization_UsesExpectedFilters()
    {
        var options = new ConvertOptions(
            Container: "mkv",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: false,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libx265",
                RateControlMode: VideoRateControlMode.ConstantQuality,
                Crf: 24,
                Bitrate: null,
                Preset: "medium",
                Tune: null,
                PixelFormat: "yuv420p",
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.SetOutput,
                Width: 1280,
                Height: 720,
                Profile: null,
                Level: null,
                GopSize: null,
                PassMode: VideoPassMode.SinglePass,
                DeinterlaceMode: VideoDeinterlaceMode.Yadif,
                CropX: 10,
                CropY: 20,
                CropWidth: 1900,
                CropHeight: 1000,
                PadToSize: true,
                PadWidth: 1280,
                PadHeight: 720,
                PadX: 0,
                PadY: 0),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "aac",
                Bitrate: "160k",
                SampleRate: null,
                Channels: null,
                ChannelLayout: null,
                NormalizationMode: AudioNormalizationMode.Loudnorm,
                LoudnessTarget: -16,
                TruePeak: -1.5,
                LoudnessRange: 11));

        var command = _builder.Build(new ConvertRequest("in.mxf", "out.mkv", options));

        Assert.Contains("yadif", command);
        Assert.Contains("crop=1900:1000:10:20", command);
        Assert.Contains("scale=1280:720", command);
        Assert.Contains("pad=1280:720:0:0:black", command);
        Assert.Contains("-af \"loudnorm=I=-16:TP=-1.5:LRA=11\"", command);
    }

    [Fact]
    public void InvalidConvertRequest_WithTwoPassAndCrf_ThrowsWithValidationErrors()
    {
        var options = new ConvertOptions(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libx264",
                RateControlMode: VideoRateControlMode.ConstantQuality,
                Crf: 20,
                Bitrate: null,
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
                PassMode: VideoPassMode.TwoPass),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "aac",
                Bitrate: "160k",
                SampleRate: null,
                Channels: null,
                ChannelLayout: null));

        var ex = Assert.Throws<InvalidOperationException>(() => _builder.Build(new ConvertRequest("in.mp4", "out.mp4", options)));
        Assert.Contains("Two-pass video encoding requires rate control mode Bitrate", ex.Message);
    }

}

public sealed class CommandBuilderStreamMappingTests
{
    private readonly CommandBuilder _builder = new();

    [Fact]
    public void BuildConvert_WithStreamMappingSubtitleCopyAndMetadata_IncludesMapMetadataAndSubtitleArgs()
    {
        var options = new ConvertOptions(
            Container: "mkv",
            OverwriteMode: OverwriteMode.Overwrite,
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
                GopSize: null,
                SourceStreamIndex: 1),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Copy,
                Codec: "copy",
                Bitrate: null,
                SampleRate: null,
                Channels: null,
                ChannelLayout: null,
                SourceStreamIndex: 0),
            Subtitle: new SubtitleOptions(SubtitleProcessingMode.Copy, 0, "eng", true),
            Metadata: new MetadataOptions(true, false, "My Title", "Author", "Commentary"));

        var command = _builder.Build(new ConvertRequest("in.mkv", "out.mkv", options));

        Assert.Contains("-map 0:v:1?", command);
        Assert.Contains("-map 0:a:0?", command);
        Assert.Contains("-map 0:s:0?", command);
        Assert.Contains("-c:s copy", command);
        Assert.Contains("-metadata:s:s:0 \"language=eng\"", command);
        Assert.Contains("-disposition:s:0 default", command);
        Assert.Contains("-map_metadata 0", command);
        Assert.Contains("-map_chapters -1", command);
        Assert.Contains("-metadata \"title=My Title\"", command);
    }


    [Fact]
    public void BuildConvert_WithEncodedSubtitlesAndMultipleMappedStreams_UsesSubtitleCodecAndMapsAllStreams()
    {
        var options = new ConvertOptions(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
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
                GopSize: null,
                SourceStreamIndex: 0),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Copy,
                Codec: "copy",
                Bitrate: null,
                SampleRate: null,
                Channels: null,
                ChannelLayout: null,
                SourceStreamIndex: 0,
                AdditionalSourceStreamIndexes: [1]),
            Subtitle: new SubtitleOptions(
                SubtitleProcessingMode.Encode,
                SourceStreamIndex: 0,
                Language: "eng",
                SetAsDefault: true,
                AdditionalSourceStreamIndexes: [2],
                Codec: "mov_text"),
            Metadata: MetadataOptions.CreateDefault());

        var command = _builder.Build(new ConvertRequest("in.mkv", "out.mp4", options));

        Assert.Contains("-map 0:v:0?", command);
        Assert.Contains("-map 0:a:0?", command);
        Assert.Contains("-map 0:a:1?", command);
        Assert.Contains("-map 0:s:0?", command);
        Assert.Contains("-map 0:s:2?", command);
        Assert.Contains("-c:s mov_text", command);
        Assert.Contains("-metadata:s:s:0 \"language=eng\"", command);
        Assert.Contains("-metadata:s:s:1 \"language=eng\"", command);
        Assert.Contains("-disposition:s:0 default", command);
    }

    [Fact]
    public void BuildConvert_WithBurnInSubtitles_UsesSubtitleFilterAndDisablesSubtitleOutput()
    {
        var options = new ConvertOptions(
            Container: "mp4",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: true,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libx264",
                RateControlMode: VideoRateControlMode.ConstantQuality,
                Crf: 20,
                Bitrate: null,
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
                GopSize: null),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Copy,
                Codec: "copy",
                Bitrate: null,
                SampleRate: null,
                Channels: null,
                ChannelLayout: null),
            Subtitle: new SubtitleOptions(SubtitleProcessingMode.BurnIn, 1),
            Metadata: MetadataOptions.CreateDefault());

        var command = _builder.Build(new ConvertRequest("movie.mkv", "out.mp4", options));

        Assert.Contains("subtitles=movie.mkv:si=1", command);
        Assert.Contains("-sn", command);
    }
}
