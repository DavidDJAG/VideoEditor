using VideoEditor.Application.Abstractions;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Toolchain;

namespace VideoEditor.Infrastructure.Services;

public sealed class PlaybackService : IPlaybackService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly IToolchainResolver _toolchainResolver;

    public PlaybackService(IProcessExecutor processExecutor, IToolchainResolver toolchainResolver)
    {
        _processExecutor = processExecutor;
        _toolchainResolver = toolchainResolver;
    }

    public async Task PlayAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        var ffplayPath = _toolchainResolver.ResolvePathsOrThrow().FfplayPath ?? "ffplay";
        await _processExecutor.RunAsync(ffplayPath, $"-autoexit \"{inputPath}\"", cancellationToken);
    }

    public Task PauseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
