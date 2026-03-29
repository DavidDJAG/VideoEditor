using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;
using VidEditor.Infrastructure.Services;

namespace VidEditor.Tests;

public sealed class ConcatCompatibilityServiceTests
{
    [Fact]
    public async Task CheckStreamCopyCompatibilityAsync_ReturnsIncompatibilityReasons()
    {
        var service = new ConcatCompatibilityService(new FakeProbeService(new Dictionary<string, MediaProbeResult>
        {
            ["a.mp4"] = Probe("a.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", 29.97, "aac", 48000, 2, "stereo", "fltp"),
            ["b.mp4"] = Probe("b.mp4", "matroska,webm", "h265", 25.0, "opus", 48000, 2, "stereo", "fltp")
        }));

        var result = await service.CheckStreamCopyCompatibilityAsync(["a.mp4", "b.mp4"]);

        Assert.False(result.IsCompatible);
        Assert.Contains(result.IncompatibilityReasons, reason => reason.Contains("container", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.IncompatibilityReasons, reason => reason.Contains("video codec", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.IncompatibilityReasons, reason => reason.Contains("audio codec", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EnsureStreamCopyCompatibilityAsync_ThrowsWithAlternativeWhenIncompatible()
    {
        var service = new ConcatCompatibilityService(new FakeProbeService(new Dictionary<string, MediaProbeResult>
        {
            ["a.mp4"] = Probe("a.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", 29.97, "aac", 48000, 2, "stereo", "fltp"),
            ["b.mp4"] = Probe("b.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", 25.0, "aac", 44100, 2, "stereo", "fltp")
        }));

        var ex = await Assert.ThrowsAsync<ConcatCompatibilityException>(() => service.EnsureStreamCopyCompatibilityAsync(["a.mp4", "b.mp4"]));

        Assert.Contains("blocked", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("transcode all inputs", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MediaProbeResult Probe(
        string path,
        string container,
        string videoCodec,
        double fps,
        string audioCodec,
        int sampleRate,
        int channels,
        string layout,
        string sampleFormat)
        => new(
            path,
            TimeSpan.FromSeconds(1),
            100,
            container,
            videoCodec,
            audioCodec,
            1,
            1,
            0,
            320,
            180,
            fps,
            sampleRate,
            channels,
            layout,
            sampleFormat,
            "{}");

    private sealed class FakeProbeService : IFfprobeService
    {
        private readonly IReadOnlyDictionary<string, MediaProbeResult> _map;

        public FakeProbeService(IReadOnlyDictionary<string, MediaProbeResult> map)
        {
            _map = map;
        }

        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
            => Task.FromResult(_map[inputPath]);
    }
}
