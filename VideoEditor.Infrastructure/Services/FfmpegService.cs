using VideoEditor.Application.Abstractions;
using VideoEditor.Application.Services;
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

        var commands = _commandBuilder.BuildCommandSequence(request);
        return ExecuteCommandSequenceAsync(commands, request, cancellationToken);
    }

    private async Task<int> ExecuteConcatWithPrecheckAsync(ConcatRequest concat, CancellationToken cancellationToken)
    {
        await _concatCompatibilityService.EnsureStreamCopyCompatibilityAsync(concat.Inputs, cancellationToken);
        var manifestPath = concat.ManifestPath ?? CommandBuilder.GetConcatManifestPath(concat.OutputPath);
        var runtimeRequest = concat with { ManifestPath = manifestPath };

        await WriteConcatManifestAsync(manifestPath, concat.Inputs, cancellationToken);

        try
        {
            var commands = _commandBuilder.BuildCommandSequence(runtimeRequest);
            return await ExecuteCommandSequenceAsync(commands, runtimeRequest, cancellationToken);
        }
        finally
        {
            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }
        }
    }


    private async Task<int> ExecuteCommandSequenceAsync(IReadOnlyList<string> commands, IFfmpegOperationRequest request, CancellationToken cancellationToken)
    {
        if (commands.Count == 0)
        {
            return 0;
        }

        try
        {
            foreach (var command in commands)
            {
                var exitCode = await ExecuteAsync(command, cancellationToken);
                if (exitCode != 0)
                {
                    return exitCode;
                }
            }

            return 0;
        }
        finally
        {
            CleanupTwoPassArtifacts(request);
        }
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

    private static Task WriteConcatManifestAsync(string manifestPath, IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        var lines = new List<string>(inputs.Count + 1) { "ffconcat version 1.0" };
        lines.AddRange(inputs.Select(static input => $"file '{EscapeConcatPath(Path.GetFullPath(input))}'"));
        return File.WriteAllLinesAsync(manifestPath, lines, cancellationToken);
    }

    private static string EscapeConcatPath(string path)
        => path.Replace("\\", "/").Replace("'", "'\\''");
}
