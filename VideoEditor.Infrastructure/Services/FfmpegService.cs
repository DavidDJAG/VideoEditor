using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Toolchain;

namespace VideoEditor.Infrastructure.Services;

public sealed class FfmpegService : IFfmpegService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ICommandBuilder _commandBuilder;
    private readonly IOperationRequestFactory _operationRequestFactory;
    private readonly IToolchainResolver _toolchainResolver;
    private readonly IConcatCompatibilityService _concatCompatibilityService;

    public FfmpegService(
        IProcessExecutor processExecutor,
        ICommandBuilder commandBuilder,
        IOperationRequestFactory operationRequestFactory,
        IToolchainResolver toolchainResolver,
        IConcatCompatibilityService concatCompatibilityService)
    {
        _processExecutor = processExecutor;
        _commandBuilder = commandBuilder;
        _operationRequestFactory = operationRequestFactory;
        _toolchainResolver = toolchainResolver;
        _concatCompatibilityService = concatCompatibilityService;
    }

    public async Task<int> ExecuteAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var toolPaths = _toolchainResolver.ResolvePathsOrThrow();
        var result = await _processExecutor.RunAsync(toolPaths.FfmpegPath, arguments, cancellationToken);
        return result.ExitCode;
    }

    public Task<int> ExecuteOperationAsync(OperationKind kind, OperationParameters operation, CancellationToken cancellationToken = default)
    {
        var request = _operationRequestFactory.Create(kind, operation);
        if (request is ConcatRequest concat)
        {
            return ExecuteConcatWithPrecheckAsync(concat, cancellationToken);
        }

        var args = _commandBuilder.Build(request);
        return ExecuteAsync(args, cancellationToken);
    }

    private async Task<int> ExecuteConcatWithPrecheckAsync(ConcatRequest concat, CancellationToken cancellationToken)
    {
        await _concatCompatibilityService.EnsureStreamCopyCompatibilityAsync(concat.Inputs, cancellationToken);
        var args = _commandBuilder.Build(concat);
        return await ExecuteAsync(args, cancellationToken);
    }
}
