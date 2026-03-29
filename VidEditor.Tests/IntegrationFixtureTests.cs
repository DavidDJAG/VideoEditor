using VidEditor.Application.Services;
using VidEditor.Domain.Models;
using VidEditor.Infrastructure.Services;

namespace VidEditor.Tests;

public sealed class IntegrationFixtureTests
{
    [Fact]
    public void CommandBuilder_TrimFixture_MatchesDeterministicOutput()
    {
        var fixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        var expected = File.ReadAllText(Path.Combine(fixturesRoot, "Commands", "trim_expected.txt")).Trim();

        var builder = new CommandBuilder();
        var request = new TrimRequest("sample_1s.mp4", "trimmed.mp4", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4));

        var actual = builder.Build(request);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProbeParser_SampleFixture_MatchesDeterministicOutput()
    {
        var fixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        var json = File.ReadAllText(Path.Combine(fixturesRoot, "Probe", "sample_1s_ffprobe.json"));
        var mediaFilePath = Path.Combine(fixturesRoot, "Probe", "sample_1s.mp4");

        var result = FfprobeJsonParser.Parse(mediaFilePath, json);

        Assert.Equal(TimeSpan.FromSeconds(1), result.Duration);
        Assert.Equal(24576, result.SizeBytes);
        Assert.Equal("mov,mp4,m4a,3gp,3g2,mj2", result.Container);
        Assert.Equal("h264", result.VideoCodec);
        Assert.Equal("aac", result.AudioCodec);
        Assert.Equal(1, result.VideoStreamCount);
        Assert.Equal(1, result.AudioStreamCount);
        Assert.Equal(0, result.SubtitleStreamCount);
        Assert.Equal(320, result.Width);
        Assert.Equal(180, result.Height);
        Assert.Equal(30000d / 1001d, result.FrameRate);
        Assert.Equal(48000, result.AudioSampleRate);
        Assert.Equal(2, result.AudioChannels);
        Assert.Equal("stereo", result.AudioChannelLayout);
        Assert.Equal("fltp", result.AudioSampleFormat);
    }
}
