using VideoEditor.Application.Abstractions;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Toolchain;
using VideoEditor.Domain.Models;

namespace VideoEditor.Infrastructure.Services;

public sealed class FfmpegJobExecutionService : IJobExecutionService
{
    private readonly ICommandBuilder _commandBuilder;
    private readonly IOperationRequestFactory _operationRequestFactory;
    private readonly IToolchainResolver _toolchainResolver;
    private readonly IProcessExecutor _processExecutor;

    public FfmpegJobExecutionService(
        ICommandBuilder commandBuilder,
        IOperationRequestFactory operationRequestFactory,
        IToolchainResolver toolchainResolver,
        IProcessExecutor processExecutor)
    {
        _commandBuilder = commandBuilder;
        _operationRequestFactory = operationRequestFactory;
        _toolchainResolver = toolchainResolver;
        _processExecutor = processExecutor;
    }

    public async Task<JobExecutionArtifact> ExecuteAsync(MediaJob job, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        OperationCatalog.TryParseLegacyOperation(job.Operation, out var kind);
        var request = _operationRequestFactory.Create(kind, job.Parameters);
        var command = _commandBuilder.Build(request);

        var toolPaths = _toolchainResolver.ResolvePathsOrThrow();
        var result = await _processExecutor.RunAsync(toolPaths.FfmpegPath, command, cancellationToken);
        var finishedAt = DateTimeOffset.UtcNow;

        var outputs = string.IsNullOrWhiteSpace(job.Parameters.OutputPath)
            ? Array.Empty<string>()
            : new[] { job.Parameters.OutputPath! };

        return new JobExecutionArtifact(
            job.Id,
            $"{toolPaths.FfmpegPath} {command}",
            result.StandardOutput,
            result.StandardError,
            result.ExitCode,
            startedAt,
            finishedAt,
            outputs);
    }
}
