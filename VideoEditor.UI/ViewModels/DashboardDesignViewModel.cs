using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels;

public sealed class DashboardDesignViewModel : DashboardViewModel
{
    public DashboardDesignViewModel() : base(new DesignToolchainCapabilitiesService())
    {
        ApplySnapshot(new ToolchainCapabilitiesSnapshot(
            DateTimeOffset.UtcNow,
            new ToolchainBinaryDiagnostic("ffmpeg", "ffmpeg", @"C:\ffmpeg\bin\ffmpeg.exe", true, false, false, null),
            new ToolchainBinaryDiagnostic("ffprobe", "ffprobe", @"C:\ffmpeg\bin\ffprobe.exe", true, false, false, null),
            null,
            "ffmpeg version 7.0-design",
            ["h264", "hevc", "prores"],
            ["d3d11va", "dxva2"],
            ["libx264", "libx265", "h264_nvenc"],
            ["aac", "libopus", "flac"],
            ["mp4", "mkv", "mov", "webm"],
            ["yuv420p", "nv12", "p010le"]));
    }

    private sealed class DesignToolchainCapabilitiesService : IToolchainCapabilitiesService
    {
        public Task<ToolchainCapabilitiesSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ToolchainCapabilitiesSnapshot(
                DateTimeOffset.UtcNow,
                new ToolchainBinaryDiagnostic("ffmpeg", "ffmpeg", "ffmpeg", false, true, false, null),
                new ToolchainBinaryDiagnostic("ffprobe", "ffprobe", "ffprobe", false, true, false, null),
                null,
                "ffmpeg version design",
                ["h264"],
                ["vaapi"],
                ["libx264", "libx265"],
                ["aac", "libopus"],
                ["mp4", "mkv", "webm"],
                ["yuv420p", "nv12"]));

        public Task<ToolchainCapabilitiesSnapshot> RefreshAsync(CancellationToken cancellationToken = default) => GetSnapshotAsync(cancellationToken);
    }
}
