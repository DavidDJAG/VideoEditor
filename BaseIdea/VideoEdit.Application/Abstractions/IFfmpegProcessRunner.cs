using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface IFfmpegProcessRunner
{
    Task<ProcessExecutionResult> RunAsync(
        CommandDefinition command,
        Action<string>? stdOutHandler,
        Action<string>? stdErrHandler,
        CancellationToken cancellationToken);
}
