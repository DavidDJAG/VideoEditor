using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;
using VidEditor.Infrastructure.Execution;
using VidEditor.Infrastructure.FileSystem;
using VidEditor.Infrastructure.Toolchain;

namespace VidEditor.Infrastructure.Services;

public sealed class FfprobeService : IFfprobeService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly IFileSystemService _fileSystem;
    private readonly IToolchainResolver _toolchainResolver;

    public FfprobeService(IProcessExecutor processExecutor, IFileSystemService fileSystem, IToolchainResolver toolchainResolver)
    {
        _processExecutor = processExecutor;
        _fileSystem = fileSystem;
        _toolchainResolver = toolchainResolver;
    }

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        var toolPaths = _toolchainResolver.ResolvePathsOrThrow();
        var result = await _processExecutor.RunAsync(toolPaths.FfprobePath, $"-v quiet -print_format json -show_format -show_streams \"{inputPath}\"", cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffprobe exited with code {result.ExitCode}: {result.StandardError}");
        }

        var fileSize = _fileSystem.Exists(inputPath) ? _fileSystem.GetLength(inputPath) : 0;
        return FfprobeJsonParser.Parse(inputPath, result.StandardOutput, fileSize);
    }
}
