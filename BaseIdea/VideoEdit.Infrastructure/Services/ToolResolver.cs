using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Infrastructure.Services;

public sealed class ToolResolver : IToolResolver
{
    private readonly Func<string?> _configuredDirectoryProvider;

    public ToolResolver(Func<string?> configuredDirectoryProvider)
    {
        _configuredDirectoryProvider = configuredDirectoryProvider;
    }

    public ToolPaths Resolve()
    {
        var configuredDirectory = _configuredDirectoryProvider();
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            var ffmpegPath = Path.Combine(configuredDirectory, "ffmpeg.exe");
            var ffprobePath = Path.Combine(configuredDirectory, "ffprobe.exe");
            if (File.Exists(ffmpegPath) && File.Exists(ffprobePath))
            {
                return new ToolPaths
                {
                    FfmpegPath = ffmpegPath,
                    FfprobePath = ffprobePath,
                    UsingConfiguredDirectory = true
                };
            }
        }

        return new ToolPaths();
    }
}
