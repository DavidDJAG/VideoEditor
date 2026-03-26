using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Settings;

namespace VideoEditor.Infrastructure.Services;

public sealed class FfmpegService : IFfmpegService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ISettingsPersistence _settingsPersistence;
    private readonly ICommandBuilder _commandBuilder;

    public FfmpegService(IProcessExecutor processExecutor, ISettingsPersistence settingsPersistence, ICommandBuilder commandBuilder)
    {
        _processExecutor = processExecutor;
        _settingsPersistence = settingsPersistence;
        _commandBuilder = commandBuilder;
    }

    public async Task<int> ExecuteAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var toolPaths = _settingsPersistence.LoadToolPaths();
        var result = await _processExecutor.RunAsync(toolPaths.FfmpegPath, arguments, cancellationToken);
        return result.ExitCode;
    }

    public Task<int> ExecuteOperationAsync(OperationParameters operation, CancellationToken cancellationToken = default)
    {
        var args = _commandBuilder.BuildTranscode(operation);
        return ExecuteAsync(args, cancellationToken);
    }
}
