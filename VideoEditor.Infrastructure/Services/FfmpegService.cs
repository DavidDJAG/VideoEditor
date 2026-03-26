using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Toolchain;

namespace VideoEditor.Infrastructure.Services;

public sealed class FfmpegService : IFfmpegService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ICommandBuilder _commandBuilder;
    private readonly IToolchainResolver _toolchainResolver;

    public FfmpegService(IProcessExecutor processExecutor, ICommandBuilder commandBuilder, IToolchainResolver toolchainResolver)
    {
        _processExecutor = processExecutor;
        _commandBuilder = commandBuilder;
        _toolchainResolver = toolchainResolver;
    }

    public async Task<int> ExecuteAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var toolPaths = _toolchainResolver.ResolvePathsOrThrow();
        var result = await _processExecutor.RunAsync(toolPaths.FfmpegPath, arguments, cancellationToken);
        return result.ExitCode;
    }

    public Task<int> ExecuteOperationAsync(OperationParameters operation, CancellationToken cancellationToken = default)
    {
        var args = _commandBuilder.BuildTranscode(operation);
        return ExecuteAsync(args, cancellationToken);
    }
}
