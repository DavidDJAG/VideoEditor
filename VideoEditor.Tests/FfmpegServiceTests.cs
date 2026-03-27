using VideoEditor.Application.Abstractions;
using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Services;
using VideoEditor.Infrastructure.Toolchain;

namespace VideoEditor.Tests;

public sealed class FfmpegServiceTests
{
    [Fact]
    public async Task ExecuteOperationAsync_Concat_WritesManifestUsesDemuxerAndDeletesManifest()
    {
        var processExecutor = new CapturingProcessExecutor();
        var toolchainResolver = new StubToolchainResolver();
        var concatCompatibilityService = new AllowingConcatCompatibilityService();
        var commandBuilder = new CommandBuilder();
        var operationRequestFactory = new OperationRequestFactory();
        var ffmpegService = new FfmpegService(
            processExecutor,
            commandBuilder,
            operationRequestFactory,
            toolchainResolver,
            concatCompatibilityService);

        var workingDirectory = Path.Combine(Path.GetTempPath(), "VideoEditor.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        var inputA = Path.Combine(workingDirectory, "a.mp4");
        var inputB = Path.Combine(workingDirectory, "b.mp4");
        var output = Path.Combine(workingDirectory, "joined.mp4");
        await File.WriteAllTextAsync(inputA, "a");
        await File.WriteAllTextAsync(inputB, "b");

        try
        {
            var exitCode = await ffmpegService.ExecuteOperationAsync(
                OperationKind.Concat,
                new OperationParameters(
                    InputPath: inputA,
                    OutputPath: output,
                    Start: null,
                    End: null,
                    SubtitleOffset: null,
                    SpeedFactor: 1.0,
                    AdditionalInputs: [],
                    Flags: new Dictionary<string, string>(),
                    EncodingProfile: null,
                    ConcatInputs: [inputA, inputB]));

            var manifestPath = output + ".ffconcat";

            Assert.Equal(0, exitCode);
            Assert.Contains($"-f concat -safe 0 -i \"{manifestPath}\"", processExecutor.Arguments);
            Assert.Contains($"\"{output}\"", processExecutor.Arguments);
            Assert.False(File.Exists(manifestPath));
            Assert.Equal([inputA, inputB], concatCompatibilityService.LastInputs);
        }
        finally
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    private sealed class CapturingProcessExecutor : IProcessExecutor
    {
        public string FileName { get; private set; } = string.Empty;

        public string Arguments { get; private set; } = string.Empty;

        public Task<ProcessExecutionResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
        {
            FileName = fileName;
            Arguments = arguments;
            return Task.FromResult(new ProcessExecutionResult(0, string.Empty, string.Empty));
        }
    }

    private sealed class StubToolchainResolver : IToolchainResolver
    {
        public ToolchainBinaryDiagnostic ResolveBinary(string toolName, string configuredValue)
            => throw new NotSupportedException();

        public (ToolchainBinaryDiagnostic Ffmpeg, ToolchainBinaryDiagnostic Ffprobe, ToolchainBinaryDiagnostic? Ffplay) ResolveAll()
            => throw new NotSupportedException();

        public ToolPaths ResolvePathsOrThrow()
            => new("ffmpeg.exe", "ffprobe.exe", null);
    }

    private sealed class AllowingConcatCompatibilityService : IConcatCompatibilityService
    {
        public IReadOnlyList<string> LastInputs { get; private set; } = [];

        public Task<ConcatCompatibilityResult> CheckStreamCopyCompatibilityAsync(IReadOnlyList<string> orderedInputs, CancellationToken cancellationToken = default)
            => Task.FromResult(ConcatCompatibilityResult.Compatible);

        public Task EnsureStreamCopyCompatibilityAsync(IReadOnlyList<string> orderedInputs, CancellationToken cancellationToken = default)
        {
            LastInputs = orderedInputs.ToArray();
            return Task.CompletedTask;
        }
    }
}
