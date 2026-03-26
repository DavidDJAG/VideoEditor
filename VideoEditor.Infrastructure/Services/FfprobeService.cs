using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.FileSystem;
using VideoEditor.Infrastructure.Settings;

namespace VideoEditor.Infrastructure.Services;

public sealed class FfprobeService : IFfprobeService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ISettingsPersistence _settingsPersistence;
    private readonly IFileSystemService _fileSystem;

    public FfprobeService(IProcessExecutor processExecutor, ISettingsPersistence settingsPersistence, IFileSystemService fileSystem)
    {
        _processExecutor = processExecutor;
        _settingsPersistence = settingsPersistence;
        _fileSystem = fileSystem;
    }

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        var toolPaths = _settingsPersistence.LoadToolPaths();
        await _processExecutor.RunAsync(toolPaths.FfprobePath, $"-v quiet -print_format json -show_format -show_streams \"{inputPath}\"", cancellationToken);

        var fileSize = _fileSystem.Exists(inputPath) ? _fileSystem.GetLength(inputPath) : 0;
        return new MediaProbeResult(inputPath, TimeSpan.Zero, fileSize, Path.GetExtension(inputPath).TrimStart('.'), 0, 0, 0, null, null, null, null, null);
    }
}
