using VideoEditor.Application.Abstractions;
using VideoEditor.Infrastructure.Toolchain;
using System.Diagnostics;
using System.Globalization;

namespace VideoEditor.Infrastructure.Services;

public sealed class PlaybackService : IPlaybackService, IDisposable
{
    private readonly IToolchainResolver _toolchainResolver;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Process? _activeProcess;
    private CancellationTokenSource? _monitorCts;

    public PlaybackService(IToolchainResolver toolchainResolver)
    {
        _toolchainResolver = toolchainResolver;
    }

    public async Task PlayAsync(
        string inputPath,
        TimeSpan? start = null,
        TimeSpan? end = null,
        double speedFactor = 1.0,
        TimeSpan? subtitleOffset = null,
        CancellationToken cancellationToken = default)
    {
        var ffplayPath = _toolchainResolver.ResolvePathsOrThrow().FfplayPath ?? "ffplay";
        var arguments = BuildArguments(inputPath, start, end, speedFactor, subtitleOffset);
        await RestartProcessAsync(ffplayPath, arguments, cancellationToken);
    }

    public async Task PlayABPreviewAsync(
        string inputPath,
        TimeSpan? aStart,
        TimeSpan? aEnd,
        TimeSpan? bStart,
        TimeSpan? bEnd,
        double speedFactor = 1.0,
        TimeSpan? subtitleOffset = null,
        CancellationToken cancellationToken = default)
    {
        var ffplayPath = _toolchainResolver.ResolvePathsOrThrow().FfplayPath ?? "ffplay";
        var concatScript = BuildAbConcatScript(inputPath, aStart, aEnd, bStart, bEnd);
        await RestartProcessAsync(ffplayPath, $"-autoexit -f lavfi {Quote(concatScript)}", cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            StopProcess_NoLock();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Wait();
        try
        {
            StopProcess_NoLock();
            _monitorCts?.Dispose();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task RestartProcessAsync(string ffplayPath, string arguments, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            StopProcess_NoLock();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffplayPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                },
                EnableRaisingEvents = true
            };

            process.Start();
            _activeProcess = process;
            _monitorCts = new CancellationTokenSource();
            _ = MonitorProcessExitAsync(process, _monitorCts.Token);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task MonitorProcessExitAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await _gate.WaitAsync(CancellationToken.None);
        try
        {
            if (ReferenceEquals(_activeProcess, process))
            {
                _activeProcess.Dispose();
                _activeProcess = null;
                _monitorCts?.Dispose();
                _monitorCts = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void StopProcess_NoLock()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;

        if (_activeProcess is null)
        {
            return;
        }

        try
        {
            if (!_activeProcess.HasExited)
            {
                _activeProcess.Kill(entireProcessTree: true);
                _activeProcess.WaitForExit(2000);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
        finally
        {
            _activeProcess.Dispose();
            _activeProcess = null;
        }
    }

    private static string BuildArguments(string inputPath, TimeSpan? start, TimeSpan? end, double speedFactor, TimeSpan? subtitleOffset)
    {
        var args = new List<string> { "-autoexit" };

        if (start.HasValue)
        {
            args.Add("-ss");
            args.Add(start.Value.ToString("c", CultureInfo.InvariantCulture));
        }

        if (end.HasValue && start.HasValue && end > start)
        {
            args.Add("-t");
            args.Add((end.Value - start.Value).ToString("c", CultureInfo.InvariantCulture));
        }

        if (subtitleOffset.HasValue)
        {
            args.Add("-sync");
            args.Add("audio");
            args.Add("-itsoffset");
            args.Add(subtitleOffset.Value.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        }

        if (Math.Abs(speedFactor - 1.0) > 0.0001)
        {
            args.Add("-vf");
            args.Add(Quote($"setpts={1d / speedFactor:0.######}*PTS"));
            args.Add("-af");
            args.Add(Quote($"atempo={Math.Clamp(speedFactor, 0.5, 2.0):0.###}"));
        }

        args.Add(Quote(inputPath));
        return string.Join(' ', args);
    }

    private static string BuildAbConcatScript(
        string inputPath,
        TimeSpan? aStart,
        TimeSpan? aEnd,
        TimeSpan? bStart,
        TimeSpan? bEnd)
    {
        return $"movie={EscapeLavfi(inputPath)}[src];[src]trim=start={ToSeconds(aStart)}:end={ToSeconds(aEnd)},setpts=PTS-STARTPTS[a];[src]trim=start={ToSeconds(bStart)}:end={ToSeconds(bEnd)},setpts=PTS-STARTPTS[b];[a][b]concat=n=2:v=1:a=0[outv]";
    }

    private static string ToSeconds(TimeSpan? value)
        => (value ?? TimeSpan.Zero).TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Quote(string value) => $"\"{value}\"";

    private static string EscapeLavfi(string value)
        => value.Replace("\\", "\\\\").Replace(":", "\\:");
}
