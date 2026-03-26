using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Services;

public sealed class ConcatCompatibilityAnalyzer : IConcatCompatibilityAnalyzer
{
    private readonly IFfprobeService _ffprobeService;

    public ConcatCompatibilityAnalyzer(IFfprobeService ffprobeService)
    {
        _ffprobeService = ffprobeService;
    }

    public async Task<ConcatCompatibilityReport> AnalyzeAsync(IReadOnlyList<string> inputPaths, CancellationToken cancellationToken)
    {
        var report = new ConcatCompatibilityReport();
        if (inputPaths.Count < 2)
        {
            report.Issues.Add(new ConcatCompatibilityIssue
            {
                Category = "Cantidad",
                FilePath = string.Empty,
                Detail = "Se requieren al menos dos archivos."
            });
            return report;
        }

        foreach (var path in inputPaths)
        {
            report.Probes.Add(await _ffprobeService.ProbeAsync(path, cancellationToken).ConfigureAwait(false));
        }

        var baseline = report.Probes[0];
        var baselineVideo = baseline.Streams.FirstOrDefault(stream => string.Equals(stream.CodecType, "video", StringComparison.OrdinalIgnoreCase));
        var baselineAudio = baseline.Streams.FirstOrDefault(stream => string.Equals(stream.CodecType, "audio", StringComparison.OrdinalIgnoreCase));

        foreach (var probe in report.Probes.Skip(1))
        {
            CompareContainer(baseline, probe, report);
            CompareVideo(baselineVideo, probe, report);
            CompareAudio(baselineAudio, probe, report);
        }

        return report;
    }

    private static void CompareContainer(MediaProbeResult baseline, MediaProbeResult current, ConcatCompatibilityReport report)
    {
        if (!string.Equals(baseline.ContainerFormat, current.ContainerFormat, StringComparison.OrdinalIgnoreCase))
        {
            report.Issues.Add(new ConcatCompatibilityIssue
            {
                Category = "Contenedor",
                FilePath = current.InputPath,
                Detail = $"Esperado {baseline.ContainerFormat ?? "n/d"}, recibido {current.ContainerFormat ?? "n/d"}."
            });
        }
    }

    private static void CompareVideo(MediaStreamInfo? baselineVideo, MediaProbeResult current, ConcatCompatibilityReport report)
    {
        var currentVideo = current.Streams.FirstOrDefault(stream => string.Equals(stream.CodecType, "video", StringComparison.OrdinalIgnoreCase));
        if (baselineVideo is null && currentVideo is null)
        {
            return;
        }

        if (baselineVideo is null || currentVideo is null)
        {
            report.Issues.Add(new ConcatCompatibilityIssue
            {
                Category = "Video",
                FilePath = current.InputPath,
                Detail = "No coincide la presencia de stream de video."
            });
            return;
        }

        CompareValue("Codec de video", baselineVideo.CodecName, currentVideo.CodecName, current.InputPath, report);
        CompareValue("Resolucion", $"{baselineVideo.Width}x{baselineVideo.Height}", $"{currentVideo.Width}x{currentVideo.Height}", current.InputPath, report);
        CompareValue("FPS", NormalizeRate(baselineVideo.FrameRate), NormalizeRate(currentVideo.FrameRate), current.InputPath, report);
        CompareValue("Pixel format", baselineVideo.PixelFormat, currentVideo.PixelFormat, current.InputPath, report);
    }

    private static void CompareAudio(MediaStreamInfo? baselineAudio, MediaProbeResult current, ConcatCompatibilityReport report)
    {
        var currentAudio = current.Streams.FirstOrDefault(stream => string.Equals(stream.CodecType, "audio", StringComparison.OrdinalIgnoreCase));
        if (baselineAudio is null && currentAudio is null)
        {
            return;
        }

        if (baselineAudio is null || currentAudio is null)
        {
            report.Issues.Add(new ConcatCompatibilityIssue
            {
                Category = "Audio",
                FilePath = current.InputPath,
                Detail = "No coincide la presencia de stream de audio."
            });
            return;
        }

        CompareValue("Codec de audio", baselineAudio.CodecName, currentAudio.CodecName, current.InputPath, report);
        CompareValue("Sample rate", baselineAudio.SampleRate, currentAudio.SampleRate, current.InputPath, report);
        CompareValue("Canales", baselineAudio.ChannelLayout, currentAudio.ChannelLayout, current.InputPath, report);
    }

    private static void CompareValue(string category, string? expected, string? actual, string filePath, ConcatCompatibilityReport report)
    {
        if (!string.Equals(expected ?? string.Empty, actual ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            report.Issues.Add(new ConcatCompatibilityIssue
            {
                Category = category,
                FilePath = filePath,
                Detail = $"Esperado {expected ?? "n/d"}, recibido {actual ?? "n/d"}."
            });
        }
    }

    private static string NormalizeRate(string? rate)
    {
        if (string.IsNullOrWhiteSpace(rate))
        {
            return string.Empty;
        }

        var parts = rate.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], out var numerator) &&
            double.TryParse(parts[1], out var denominator) &&
            denominator != 0)
        {
            return (numerator / denominator).ToString("0.###");
        }

        return rate;
    }
}
