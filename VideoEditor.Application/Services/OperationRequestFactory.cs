using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Services;

public sealed class OperationRequestFactory : IOperationRequestFactory
{
    public IFfmpegOperationRequest Create(OperationKind kind, OperationParameters parameters)
        => kind switch
        {
            OperationKind.Trim => new TrimRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.Start ?? TimeSpan.Zero,
                parameters.End ?? throw new InvalidOperationException("Trim requires End time.")),
            OperationKind.ExtractAudio => new ExtractAudioRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.Flags.TryGetValue("audioCodec", out var audioCodec) ? audioCodec : "copy"),
            OperationKind.ExtractVideo => new ExtractVideoRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.Flags.TryGetValue("videoCodec", out var videoCodec) ? videoCodec : "copy"),
            OperationKind.Convert => new ConvertRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.EncodingProfile ?? throw new InvalidOperationException("Convert requires EncodingProfile.")),
            OperationKind.Concat => new ConcatRequest(
                parameters.ConcatInputs ?? throw new InvalidOperationException("Concat requires ConcatInputs."),
                RequireOutput(parameters, kind)),
            OperationKind.NormalizeLoudness => new NormalizeLoudnessRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind)),
            OperationKind.Subtitle => new SubtitleRequest(
                parameters.InputPath,
                parameters.Flags.TryGetValue("subtitlePath", out var subtitlePath) ? subtitlePath : throw new InvalidOperationException("Subtitle requires subtitlePath flag."),
                RequireOutput(parameters, kind),
                parameters.Flags.TryGetValue("subtitleMode", out var subtitleMode) && subtitleMode.Equals("mux", StringComparison.OrdinalIgnoreCase)
                    ? SubtitleMode.Mux
                    : SubtitleMode.BurnIn,
                parameters.Flags.TryGetValue("language", out var language) ? language : null),
            OperationKind.ThumbnailContactSheet => new ThumbnailContactSheetRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.Flags.TryGetValue("contactSheet", out var contactSheet) && bool.TryParse(contactSheet, out var generateContactSheet) && generateContactSheet),
            OperationKind.AudioChannelMapResample => new AudioChannelMapResampleRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.Flags.TryGetValue("channelLayout", out var channelLayout) ? channelLayout : throw new InvalidOperationException("Audio map requires channelLayout flag."),
                parameters.Flags.TryGetValue("sampleRate", out var sampleRateRaw) && int.TryParse(sampleRateRaw, out var sampleRate) ? sampleRate : throw new InvalidOperationException("Audio map requires sampleRate flag.")),
            OperationKind.WatermarkOverlay => new WatermarkOverlayRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.Flags.TryGetValue("watermarkImagePath", out var imagePath) ? imagePath : null,
                parameters.Flags.TryGetValue("watermarkText", out var text) ? text : null,
                parameters.Flags.TryGetValue("position", out var position) ? position : "10:10"),
            OperationKind.SpeedFramerate => new SpeedFramerateRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.SpeedFactor),
            OperationKind.SegmentHls => new SegmentHlsRequest(
                parameters.InputPath,
                RequireOutput(parameters, kind),
                parameters.Flags.TryGetValue("segmentFilePattern", out var segmentFilePattern) ? segmentFilePattern : throw new InvalidOperationException("HLS requires segmentFilePattern flag.")),
            _ => throw new NotSupportedException($"Unsupported operation kind: {kind}")
        };

    private static string RequireOutput(OperationParameters parameters, OperationKind kind)
        => parameters.OutputPath ?? throw new InvalidOperationException($"{kind} requires OutputPath.");
}
