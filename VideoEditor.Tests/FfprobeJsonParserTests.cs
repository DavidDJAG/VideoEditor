using VideoEditor.Infrastructure.Services;

namespace VideoEditor.Tests;

public sealed class FfprobeJsonParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsExpectedProbeResult()
    {
        var json = """
            {
              "format": { "format_name": "matroska,webm", "duration": "2.5", "size": "2048" },
              "streams": [
                { "codec_type": "video", "width": 1920, "height": 1080, "avg_frame_rate": "60000/1001" },
                { "codec_type": "audio", "sample_rate": "44100", "channels": 2 },
                { "codec_type": "subtitle" }
              ]
            }
            """;

        var result = FfprobeJsonParser.Parse("in.webm", json);

        Assert.Equal(TimeSpan.FromSeconds(2.5), result.Duration);
        Assert.Equal(2048, result.SizeBytes);
        Assert.Equal("matroska,webm", result.Container);
        Assert.Equal(1, result.VideoStreamCount);
        Assert.Equal(1, result.AudioStreamCount);
        Assert.Equal(1, result.SubtitleStreamCount);
        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(60000d / 1001d, result.FrameRate);
        Assert.Equal(44100, result.AudioSampleRate);
        Assert.Equal(2, result.AudioChannels);
    }

    [Fact]
    public void Parse_MissingOptionalFields_UsesFallbackValues()
    {
        var json = "{\"streams\":[]}";

        var result = FfprobeJsonParser.Parse("sample.mp4", json, fallbackSizeBytes: 1234);

        Assert.Equal("mp4", result.Container);
        Assert.Equal(1234, result.SizeBytes);
        Assert.Equal(TimeSpan.Zero, result.Duration);
    }
}
