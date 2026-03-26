using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.FileSystem;
using VideoEditor.Infrastructure.Toolchain;

namespace VideoEditor.Infrastructure.Services;

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
        await _processExecutor.RunAsync(toolPaths.FfprobePath, $"-v quiet -print_format json -show_format -show_streams \"{inputPath}\"", cancellationToken);

        var fileSize = _fileSystem.Exists(inputPath) ? _fileSystem.GetLength(inputPath) : 0;
        return new MediaProbeResult(inputPath, TimeSpan.Zero, fileSize, Path.GetExtension(inputPath).TrimStart('.'), 0, 0, 0, null, null, null, null, null);
    }
}
