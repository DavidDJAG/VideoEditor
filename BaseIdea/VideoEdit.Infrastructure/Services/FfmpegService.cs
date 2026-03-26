using System.Globalization;
using System.Text.RegularExpressions;
using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Infrastructure.Services;

public sealed partial class FfmpegService : IFfmpegService
{
    private readonly IFfmpegCommandBuilder _commandBuilder;
    private readonly IFfmpegProcessRunner _processRunner;
    private readonly IToolResolver _toolResolver;

    public FfmpegService(IFfmpegCommandBuilder commandBuilder, IFfmpegProcessRunner processRunner, IToolResolver toolResolver)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
        _toolResolver = toolResolver;
    }

    public event EventHandler<FfmpegProgressInfo>? ProgressChanged;

    public string BuildCommandPreview(MediaJobRequest request)
    {
        var command = _commandBuilder.Build(request, _toolResolver.Resolve());
        return command.CommandText;
    }

    public async Task<MediaJobResult> ExecuteJobAsync(MediaJob job, CancellationToken cancellationToken)
    {
        var preparedRequest = await PrepareRequestAsync(job.Request, cancellationToken).ConfigureAwait(false);
        var command = _commandBuilder.Build(preparedRequest, _toolResolver.Resolve());
        var startedAt = DateTimeOffset.Now;

        var execution = await _processRunner.RunAsync(
            command,
            _ => { },
            line => ReportProgress(preparedRequest, line),
            cancellationToken).ConfigureAwait(false);

        return new MediaJobResult
        {
            Success = execution.ExitCode == 0 && !execution.WasCancelled,
            Cancelled = execution.WasCancelled,
            ExitCode = execution.ExitCode,
            CommandText = command.CommandText,
            StandardOutput = execution.StandardOutput,
            StandardError = execution.StandardError,
            OutputPath = preparedRequest.OutputPath,
            StartedAt = startedAt,
            FinishedAt = DateTimeOffset.Now
        };
    }

    private static async Task<MediaJobRequest> PrepareRequestAsync(MediaJobRequest request, CancellationToken cancellationToken)
    {
        if (request.JobType != MediaJobType.Concat)
        {
            return request;
        }

        var concatPath = request.OutputPath + ".concat.txt";
        var lines = request.InputPaths.Select(path => $"file '{path.Replace("'", "'\\''")}'").ToArray();
        await File.WriteAllLinesAsync(concatPath, lines, cancellationToken).ConfigureAwait(false);
        return request;
    }

    private void ReportProgress(MediaJobRequest request, string line)
    {
        var match = ProgressRegex().Match(line);
        if (!match.Success)
        {
            return;
        }

        TimeSpan? processed = null;
        if (TimeSpan.TryParseExact(match.Groups["time"].Value, @"hh\:mm\:ss\.ff", CultureInfo.InvariantCulture, out var parsed)
            || TimeSpan.TryParse(match.Groups["time"].Value, CultureInfo.InvariantCulture, out parsed))
        {
            processed = parsed;
        }

        double? percentage = null;
        if (request.TimeRange?.Duration is { } duration && processed is not null && duration > TimeSpan.Zero)
        {
            percentage = Math.Min(100d, processed.Value.TotalMilliseconds / duration.TotalMilliseconds * 100d);
        }

        ProgressChanged?.Invoke(this, new FfmpegProgressInfo
        {
            ProcessedTime = processed,
            FramesPerSecond = match.Groups["fps"].Value,
            Speed = match.Groups["speed"].Value,
            Percentage = percentage,
            RawLine = line
        });
    }

    [GeneratedRegex(@"fps=\s*(?<fps>[^\s]+).*time=(?<time>\d{2}:\d{2}:\d{2}\.\d{2}).*speed=\s*(?<speed>[^\s]+)")]
    private static partial Regex ProgressRegex();
}
