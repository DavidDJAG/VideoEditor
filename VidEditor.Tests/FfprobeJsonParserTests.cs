using VidEditor.Infrastructure.Services;

namespace VidEditor.Tests;

public sealed class FfprobeJsonParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsExpectedProbeResult()
    {
        var json = """
            {
              "format": { "format_name": "matroska,webm", "duration": "2.5", "size": "2048" },
              "streams": [
                { "index": 0, "codec_type": "video", "codec_name": "h264", "width": 1920, "height": 1080, "avg_frame_rate": "60000/1001" },
                { "index": 1, "codec_type": "audio", "codec_name": "aac", "sample_rate": "44100", "channels": 2, "channel_layout": "stereo", "tags": { "language": "eng" } },
                { "index": 2, "codec_type": "subtitle", "codec_name": "subrip", "tags": { "language": "spa", "title": "Full captions" }, "disposition": { "default": 1, "forced": 0 } }
              ]
            }
            """;

        var result = FfprobeJsonParser.Parse("in.webm", json);

        Assert.Equal(TimeSpan.FromSeconds(2.5), result.Duration);
        Assert.Equal(2048, result.SizeBytes);
        Assert.Equal("matroska,webm", result.Container);
        Assert.Equal("h264", result.VideoCodec);
        Assert.Equal("aac", result.AudioCodec);
        Assert.Equal(1, result.VideoStreamCount);
        Assert.Equal(1, result.AudioStreamCount);
        Assert.Equal(1, result.SubtitleStreamCount);
        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(60000d / 1001d, result.FrameRate);
        Assert.Equal(44100, result.AudioSampleRate);
        Assert.Equal(2, result.AudioChannels);
        Assert.Equal("stereo", result.AudioChannelLayout);
        Assert.Equal(3, result.StreamInfos.Count);
        Assert.Equal(0, result.StreamInfos[0].TypeIndex);
        Assert.Equal("eng", result.StreamInfos[1].Language);
        Assert.Equal("Full captions", result.StreamInfos[2].Title);
        Assert.True(result.StreamInfos[2].IsDefault);
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
