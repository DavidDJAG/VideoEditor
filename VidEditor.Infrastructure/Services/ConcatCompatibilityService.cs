using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.Infrastructure.Services;

public sealed class ConcatCompatibilityService : IConcatCompatibilityService
{
    private readonly IFfprobeService _ffprobeService;

    public ConcatCompatibilityService(IFfprobeService ffprobeService)
    {
        _ffprobeService = ffprobeService;
    }

    public async Task<ConcatCompatibilityResult> CheckStreamCopyCompatibilityAsync(IReadOnlyList<string> orderedInputs, CancellationToken cancellationToken = default)
    {
        if (orderedInputs is not { Count: >= 2 })
        {
            return new ConcatCompatibilityResult(false, ["Concat requires at least 2 ordered inputs."]);
        }

        var probes = new List<MediaProbeResult>(orderedInputs.Count);
        for (var i = 0; i < orderedInputs.Count; i++)
        {
            var input = orderedInputs[i];
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ConcatCompatibilityResult(false, [$"Input #{i + 1} is empty. Ordered input list cannot contain blank items."]);
            }

            probes.Add(await _ffprobeService.ProbeAsync(input, cancellationToken));
        }

        var baseline = probes[0];
        var reasons = new List<string>();

        for (var i = 1; i < probes.Count; i++)
        {
            var current = probes[i];
            var indexLabel = i + 1;

            if (!StringEqualsNormalized(baseline.Container, current.Container))
            {
                reasons.Add($"Input #{indexLabel}: container '{current.Container}' differs from input #1 '{baseline.Container}'.");
            }

            if (!StringEqualsNormalized(baseline.VideoCodec, current.VideoCodec))
            {
                reasons.Add($"Input #{indexLabel}: video codec '{current.VideoCodec ?? "none"}' differs from input #1 '{baseline.VideoCodec ?? "none"}'.");
            }

            if (!NullableEqualsRounded(baseline.FrameRate, current.FrameRate, 0.01))
            {
                reasons.Add($"Input #{indexLabel}: fps '{FormatFps(current.FrameRate)}' differs from input #1 '{FormatFps(baseline.FrameRate)}'.");
            }

            if (!StringEqualsNormalized(baseline.AudioCodec, current.AudioCodec))
            {
                reasons.Add($"Input #{indexLabel}: audio codec '{current.AudioCodec ?? "none"}' differs from input #1 '{baseline.AudioCodec ?? "none"}'.");
            }

            if (baseline.AudioSampleRate != current.AudioSampleRate)
            {
                reasons.Add($"Input #{indexLabel}: audio sample rate '{current.AudioSampleRate?.ToString() ?? "none"}' differs from input #1 '{baseline.AudioSampleRate?.ToString() ?? "none"}'.");
            }

            if (baseline.AudioChannels != current.AudioChannels)
            {
                reasons.Add($"Input #{indexLabel}: audio channels '{current.AudioChannels?.ToString() ?? "none"}' differs from input #1 '{baseline.AudioChannels?.ToString() ?? "none"}'.");
            }

            if (!StringEqualsNormalized(baseline.AudioChannelLayout, current.AudioChannelLayout))
            {
                reasons.Add($"Input #{indexLabel}: audio channel layout '{current.AudioChannelLayout ?? "none"}' differs from input #1 '{baseline.AudioChannelLayout ?? "none"}'.");
            }

            if (!StringEqualsNormalized(baseline.AudioSampleFormat, current.AudioSampleFormat))
            {
                reasons.Add($"Input #{indexLabel}: audio sample format '{current.AudioSampleFormat ?? "none"}' differs from input #1 '{baseline.AudioSampleFormat ?? "none"}'.");
            }
        }

        return reasons.Count == 0
            ? ConcatCompatibilityResult.Compatible
            : new ConcatCompatibilityResult(false, reasons);
    }

    public async Task EnsureStreamCopyCompatibilityAsync(IReadOnlyList<string> orderedInputs, CancellationToken cancellationToken = default)
    {
        var report = await CheckStreamCopyCompatibilityAsync(orderedInputs, cancellationToken);
        if (!report.IsCompatible)
        {
            throw new ConcatCompatibilityException(report.IncompatibilityReasons);
        }
    }

    private static bool StringEqualsNormalized(string? left, string? right)
        => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool NullableEqualsRounded(double? left, double? right, double tolerance)
    {
        if (!left.HasValue && !right.HasValue)
        {
            return true;
        }

        if (!left.HasValue || !right.HasValue)
        {
            return false;
        }

        return Math.Abs(left.Value - right.Value) <= tolerance;
    }

    private static string FormatFps(double? value)
        => value.HasValue ? value.Value.ToString("0.###") : "none";
}
