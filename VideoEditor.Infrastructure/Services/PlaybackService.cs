using VideoEditor.Application.Abstractions;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Settings;

namespace VideoEditor.Infrastructure.Services;

public sealed class PlaybackService : IPlaybackService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ISettingsPersistence _settingsPersistence;

    public PlaybackService(IProcessExecutor processExecutor, ISettingsPersistence settingsPersistence)
    {
        _processExecutor = processExecutor;
        _settingsPersistence = settingsPersistence;
    }

    public async Task PlayAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        var ffplayPath = _settingsPersistence.LoadToolPaths().FfplayPath ?? "ffplay";
        await _processExecutor.RunAsync(ffplayPath, $"-autoexit \"{inputPath}\"", cancellationToken);
    }

    public Task PauseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
