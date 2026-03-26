using VideoEditor.Application.Abstractions;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Toolchain;
using VideoEditor.Domain.Models;

namespace VideoEditor.Infrastructure.Services;

public sealed class FfmpegJobExecutionService : IJobExecutionService
{
    private readonly ICommandBuilder _commandBuilder;
    private readonly IToolchainResolver _toolchainResolver;
    private readonly IProcessExecutor _processExecutor;

    public FfmpegJobExecutionService(
        ICommandBuilder commandBuilder,
        IToolchainResolver toolchainResolver,
        IProcessExecutor processExecutor)
    {
        _commandBuilder = commandBuilder;
        _toolchainResolver = toolchainResolver;
        _processExecutor = processExecutor;
    }

    public async Task<JobExecutionArtifact> ExecuteAsync(MediaJob job, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var command = job.Operation.ToLowerInvariant() switch
        {
            "trim" => _commandBuilder.BuildTrim(job.Parameters),
            "concat" => _commandBuilder.BuildConcat(job.Parameters),
            _ => _commandBuilder.BuildTranscode(job.Parameters)
        };

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
