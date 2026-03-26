using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

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
        var command = _builder.Build(new ConcatRequest(["a.mp4", "b.mp4"], "joined.mp4"));

        Assert.Equal("-y -i \"concat:a.mp4|b.mp4\" -c copy \"joined.mp4\"", command);
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

        Assert.Equal("-y -i \"concat:seg1.mp4|seg2.mp4|seg3.mp4\" -c copy \"joined.mp4\"", command);
    }

    [Fact]
    public void InvalidRequest_ThrowsWithValidationErrors()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _builder.Build(new NormalizeLoudnessRequest("", "", 1)));
        Assert.Contains("Invalid operation request", ex.Message);
    }
}
