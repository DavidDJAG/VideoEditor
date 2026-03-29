using VidEditor.Application.Abstractions;
using VidEditor.Infrastructure.Execution;
using VidEditor.Infrastructure.Toolchain;
using VidEditor.Domain.Models;
using VidEditor.Application.Services;

namespace VidEditor.Infrastructure.Services;

public sealed class FfmpegJobExecutionService : IJobExecutionService
{
    private readonly ICommandBuilder _commandBuilder;
    private readonly IOperationRequestFactory _operationRequestFactory;
    private readonly IToolchainResolver _toolchainResolver;
    private readonly IProcessExecutor _processExecutor;
    private readonly IConcatCompatibilityService _concatCompatibilityService;

    public FfmpegJobExecutionService(
        ICommandBuilder commandBuilder,
        IOperationRequestFactory operationRequestFactory,
        IToolchainResolver toolchainResolver,
        IProcessExecutor processExecutor,
        IConcatCompatibilityService concatCompatibilityService)
    {
        _commandBuilder = commandBuilder;
        _operationRequestFactory = operationRequestFactory;
        _toolchainResolver = toolchainResolver;
        _processExecutor = processExecutor;
        _concatCompatibilityService = concatCompatibilityService;
    }

    public async Task<JobExecutionArtifact> ExecuteAsync(MediaJob job, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        OperationCatalog.TryParseLegacyOperation(job.Operation, out var kind);
        var request = _operationRequestFactory.Create(kind, job.Parameters);
        var commands = _commandBuilder.BuildCommandSequence(request);
        var command = string.Join($"{Environment.NewLine}{Environment.NewLine}", commands);

        var toolPaths = _toolchainResolver.ResolvePathsOrThrow();
        if (request is ConcatRequest concat)
        {
            try
            {
                await _concatCompatibilityService.EnsureStreamCopyCompatibilityAsync(concat.Inputs, cancellationToken);
            }
            catch (ConcatCompatibilityException ex)
            {
                var blockedAt = DateTimeOffset.UtcNow;
                return new JobExecutionArtifact(
                    job.Id,
                    $"{toolPaths.FfmpegPath} {command}",
                    string.Empty,
                    ex.Message,
                    -1,
                    startedAt,
                    blockedAt,
                    Array.Empty<string>());
            }
        }

        ProcessExecutionResult? lastResult = null;
        var stdout = new List<string>();
        var stderr = new List<string>();

        try
        {
            foreach (var sequenceCommand in commands)
            {
                lastResult = await _processExecutor.RunAsync(toolPaths.FfmpegPath, sequenceCommand, cancellationToken);
                if (!string.IsNullOrWhiteSpace(lastResult.StandardOutput))
                {
                    stdout.Add(lastResult.StandardOutput);
                }

                if (!string.IsNullOrWhiteSpace(lastResult.StandardError))
                {
                    stderr.Add(lastResult.StandardError);
                }

                if (lastResult.ExitCode != 0)
                {
                    break;
                }
            }
        }
        finally
        {
            CleanupTwoPassArtifacts(request);
        }

        var finishedAt = DateTimeOffset.UtcNow;
        lastResult ??= new ProcessExecutionResult(0, string.Empty, string.Empty);

        var outputs = string.IsNullOrWhiteSpace(job.Parameters.OutputPath)
            ? Array.Empty<string>()
            : new[] { job.Parameters.OutputPath! };

        return new JobExecutionArtifact(
            job.Id,
            $"{toolPaths.FfmpegPath} {command}",
            string.Join(Environment.NewLine + Environment.NewLine, stdout),
            string.Join(Environment.NewLine + Environment.NewLine, stderr),
            lastResult.ExitCode,
            startedAt,
            finishedAt,
            outputs);
    }

    private static void CleanupTwoPassArtifacts(IFfmpegOperationRequest request)
    {
        if (request is not ConvertRequest { ConvertOptions.Video.PassMode: VideoPassMode.TwoPass } convert)
        {
            return;
        }

        foreach (var path in CommandBuilder.GetTwoPassLogArtifacts(convert.OutputPath))
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
