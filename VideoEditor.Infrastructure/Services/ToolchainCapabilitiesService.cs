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
            var encodersResult = await _processExecutor.RunAsync(ffmpeg.ResolvedPath, "-hide_banner -encoders", cancellationToken);
            var muxersResult = await _processExecutor.RunAsync(ffmpeg.ResolvedPath, "-hide_banner -muxers", cancellationToken);
            var pixelFormatsResult = await _processExecutor.RunAsync(ffmpeg.ResolvedPath, "-hide_banner -pix_fmts", cancellationToken);
            var hwAccelResult = await _processExecutor.RunAsync(ffmpeg.ResolvedPath, "-hide_banner -hwaccels", cancellationToken);

            var versionOutput = MergeOutput(versionResult);
            var codecsOutput = MergeOutput(codecsResult);
            var encodersOutput = MergeOutput(encodersResult);
            var muxersOutput = MergeOutput(muxersResult);
            var pixelFormatsOutput = MergeOutput(pixelFormatsResult);
            var hwAccelOutput = MergeOutput(hwAccelResult);

            var snapshot = new ToolchainCapabilitiesSnapshot(
                DateTimeOffset.UtcNow,
                ffmpeg,
                ffprobe,
                ffplay,
                ParseVersion(versionOutput),
                ParseVideoCodecs(codecsOutput),
                ParseHardwareAcceleration(hwAccelOutput),
                ParseEncoders(encodersOutput, 'V'),
                ParseEncoders(encodersOutput, 'A'),
                ParseMuxers(muxersOutput),
                ParsePixelFormats(pixelFormatsOutput),
                ParseEncoders(encodersOutput, 'S'));

            _cached = snapshot;
            return snapshot;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string MergeOutput(ProcessExecutionResult result)
        => string.Join(
            Environment.NewLine,
            new[] { result.StandardOutput, result.StandardError }
                .Where(static value => !string.IsNullOrWhiteSpace(value)));

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
            .Where(static line => line.Length > 8 && !line.StartsWith('-') && !line.StartsWith("Codecs:", StringComparison.OrdinalIgnoreCase))
            .Select(static line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(static parts => parts.Length >= 2 && parts[0].Contains('V'))
            .Select(static parts => parts[1])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static codec => codec, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return codecs;
    }

    private static IReadOnlyList<string> ParseEncoders(string output, char streamType)
    {
        var encoders = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 8
                && !line.StartsWith('-')
                && !line.StartsWith("Encoders:", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("------", StringComparison.OrdinalIgnoreCase))
            .Select(static line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2 && parts[0].Contains(streamType))
            .Select(static parts => parts[1])
            .Where(static encoder => encoder.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static encoder => encoder, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return encoders;
    }

    private static IReadOnlyList<string> ParseMuxers(string output)
    {
        var muxers = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 3
                && !line.StartsWith('-')
                && !line.StartsWith("File formats:", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("Muxers:", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("Demuxers:", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("D. =", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith(".E =", StringComparison.OrdinalIgnoreCase))
            .Select(static line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(static parts => parts.Length >= 2 && parts[0].Contains('E'))
            .SelectMany(static parts => parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(static muxer => muxer.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static muxer => muxer, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return muxers;
    }

    private static IReadOnlyList<string> ParsePixelFormats(string output)
    {
        var pixelFormats = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 8
                && !line.StartsWith('-')
                && !line.StartsWith("Pixel formats:", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("FLAGS", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("I....", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith(".O...", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("..H..", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("...P.", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("....B", StringComparison.OrdinalIgnoreCase))
            .Select(static line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(static parts => parts.Length >= 2 && parts[0].All(ch => char.IsLetterOrDigit(ch) || ch == '.'))
            .Select(static parts => parts[1])
            .Where(static pixelFormat => pixelFormat.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static pixelFormat => pixelFormat, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return pixelFormats;
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
            .OrderBy(static method => method, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return methods;
    }
}
