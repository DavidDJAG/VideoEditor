using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class CommandBuilderTests
{
    private readonly CommandBuilder _builder = new();

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
    public void InvalidRequest_ThrowsWithValidationErrors()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _builder.Build(new NormalizeLoudnessRequest("", "", 1)));
        Assert.Contains("Invalid operation request", ex.Message);
    }
}
