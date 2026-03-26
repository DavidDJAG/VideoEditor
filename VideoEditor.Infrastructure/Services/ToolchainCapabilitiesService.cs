using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.Toolchain;

namespace VideoEditor.Infrastructure.Services;

public sealed class ToolchainCapabilitiesService : IToolchainCapabilitiesService
{
    private readonly IToolchainResolver _toolchainResolver;
    private readonly IProcessExecutor _processExecutor;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ToolchainCapabilitiesSnapshot? _cached;

    public ToolchainCapabilitiesService(IToolchainResolver toolchainResolver, IProcessExecutor processExecutor)
    {
        _toolchainResolver = toolchainResolver;
        _processExecutor = processExecutor;
    }

    public async Task<ToolchainCapabilitiesSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        return await RefreshAsync(cancellationToken);
    }

    public async Task<ToolchainCapabilitiesSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var (ffmpeg, ffprobe, ffplay) = _toolchainResolver.ResolveAll();
            if (ffmpeg.IsMissing)
            {
                throw new ToolchainNotFoundException(ffmpeg.MissingRemediation ?? "ffmpeg binary is missing.");
            }

            if (ffprobe.IsMissing)
            {
                throw new ToolchainNotFoundException(ffprobe.MissingRemediation ?? "ffprobe binary is missing.");
            }

            var versionResult = await _processExecutor.RunAsync(ffmpeg.ResolvedPath, "-version", cancellationToken);
            var codecsResult = await _processExecutor.RunAsync(ffmpeg.ResolvedPath, "-hide_banner -codecs", cancellationToken);
            var hwAccelResult = await _processExecutor.RunAsync(ffmpeg.ResolvedPath, "-hide_banner -hwaccels", cancellationToken);

            var snapshot = new ToolchainCapabilitiesSnapshot(
                DateTimeOffset.UtcNow,
                ffmpeg,
                ffprobe,
                ffplay,
                ParseVersion(versionResult.Output),
                ParseVideoCodecs(codecsResult.Output),
                ParseHardwareAcceleration(hwAccelResult.Output));

            _cached = snapshot;
            return snapshot;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string ParseVersion(string output)
    {
        var firstLine = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstLine) ? "Unknown" : firstLine;
    }

    private static IReadOnlyList<string> ParseVideoCodecs(string output)
    {
        var codecs = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 8 && line[0] != '-' && char.IsLetter(line[0]))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2 && parts[0].Contains('V'))
            .Select(parts => parts[1])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(codec => codec)
            .ToList();

        return codecs;
    }

    private static IReadOnlyList<string> ParseHardwareAcceleration(string output)
    {
        var methods = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !line.Equals("Hardware acceleration methods:", StringComparison.OrdinalIgnoreCase))
            .Where(line => !line.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            .Where(line => line.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(method => method)
            .ToList();

        return methods;
    }
}
