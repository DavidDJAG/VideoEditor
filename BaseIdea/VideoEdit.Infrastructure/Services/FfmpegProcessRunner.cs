using System.Diagnostics;
using System.Text;
using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Infrastructure.Services;

public sealed class FfmpegProcessRunner : IFfmpegProcessRunner
{
    public async Task<ProcessExecutionResult> RunAsync(
        CommandDefinition command,
        Action<string>? stdOutHandler,
        Action<string>? stdErrHandler,
        CancellationToken cancellationToken)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command.FileName,
                Arguments = command.Arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            stdout.AppendLine(args.Data);
            stdOutHandler?.Invoke(args.Data);
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            stderr.AppendLine(args.Data);
            stdErrHandler?.Invoke(args.Data);
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("No se pudo iniciar el proceso.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
        });

        var exitCode = await tcs.Task.ConfigureAwait(false);
        return new ProcessExecutionResult
        {
            ExitCode = exitCode,
            WasCancelled = cancellationToken.IsCancellationRequested,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString()
        };
    }
}
