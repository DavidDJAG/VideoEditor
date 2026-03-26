using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels;

public sealed class DashboardDesignViewModel : DashboardViewModel
{
    public DashboardDesignViewModel() : base(new DesignToolchainCapabilitiesService())
    {
        ApplySnapshot(new ToolchainCapabilitiesSnapshot(
            DateTimeOffset.UtcNow,
            new ToolchainBinaryDiagnostic("ffmpeg", "ffmpeg", @"C:\\ffmpeg\\bin\\ffmpeg.exe", true, false, false, null),
            new ToolchainBinaryDiagnostic("ffprobe", "ffprobe", @"C:\\ffmpeg\\bin\\ffprobe.exe", true, false, false, null),
            null,
            "ffmpeg version 7.0-design",
            ["h264", "hevc", "prores"],
            ["dxva2", "d3d11va"]));
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
                ["vaapi"]));

        public Task<ToolchainCapabilitiesSnapshot> RefreshAsync(CancellationToken cancellationToken = default) => GetSnapshotAsync(cancellationToken);
    }
}
