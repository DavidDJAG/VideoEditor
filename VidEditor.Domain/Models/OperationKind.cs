namespace VidEditor.Domain.Models;

public enum OperationPhase
{
    V1Functional,
    V11Plus
}

public enum OperationKind
{
    Trim,
    ExtractAudio,
    ExtractVideo,
    Convert,
    Concat,
    NormalizeLoudness,
    Subtitle,
    ThumbnailContactSheet,
    AudioChannelMapResample,
    WatermarkOverlay,
    SpeedFramerate,
    SegmentHls
}

public sealed record OperationDescriptor(
    OperationKind Kind,
    string DisplayName,
    Type RequestType,
    OperationPhase Phase,
    IReadOnlyList<string> KeyValidations);

public static class OperationCatalog
{
    private static readonly IReadOnlyDictionary<OperationKind, OperationDescriptor> Catalog =
        new Dictionary<OperationKind, OperationDescriptor>
        {
            [OperationKind.Trim] = new(
                OperationKind.Trim,
                "Cut/Trim",
                typeof(TrimRequest),
                OperationPhase.V1Functional,
                ["InputPath/OutputPath requeridos", "End > Start", "Start >= 0"]),
            [OperationKind.ExtractAudio] = new(
                OperationKind.ExtractAudio,
                "Split Audio",
                typeof(ExtractAudioRequest),
                OperationPhase.V1Functional,
                ["InputPath/OutputPath requeridos", "AudioCodec no vacío"]),
            [OperationKind.ExtractVideo] = new(
                OperationKind.ExtractVideo,
                "Split Video",
                typeof(ExtractVideoRequest),
                OperationPhase.V1Functional,
                ["InputPath/OutputPath requeridos", "VideoCodec no vacío"]),
            [OperationKind.Convert] = new(
                OperationKind.Convert,
                "Convert/Transcode",
                typeof(ConvertRequest),
                OperationPhase.V11Plus,
                ["InputPath/OutputPath requeridos", "ConvertOptions requerido"]),
            [OperationKind.Concat] = new(
                OperationKind.Concat,
                "Join/Concat",
                typeof(ConcatRequest),
                OperationPhase.V1Functional,
                ["Mínimo 2 inputs", "OutputPath requerido"]),
            [OperationKind.NormalizeLoudness] = new(
                OperationKind.NormalizeLoudness,
                "Normalize Loudness",
                typeof(NormalizeLoudnessRequest),
                OperationPhase.V11Plus,
                ["I/TP <= 0", "LRA > 0"]),
            [OperationKind.Subtitle] = new(
                OperationKind.Subtitle,
                "Subtitles",
                typeof(SubtitleRequest),
                OperationPhase.V11Plus,
                ["Input/Subtitles/Output requeridos"]),
            [OperationKind.ThumbnailContactSheet] = new(
                OperationKind.ThumbnailContactSheet,
                "Thumbnails/Contact Sheet",
                typeof(ThumbnailContactSheetRequest),
                OperationPhase.V11Plus,
                ["FrameInterval > 0", "Rows/Columns > 0 cuando aplica"]),
            [OperationKind.AudioChannelMapResample] = new(
                OperationKind.AudioChannelMapResample,
                "Audio Channel Map + Resample",
                typeof(AudioChannelMapResampleRequest),
                OperationPhase.V11Plus,
                ["ChannelLayout requerido", "SampleRate > 0"]),
            [OperationKind.WatermarkOverlay] = new(
                OperationKind.WatermarkOverlay,
                "Watermark",
                typeof(WatermarkOverlayRequest),
                OperationPhase.V11Plus,
                ["Input/Output requeridos", "Imagen o texto obligatorio"]),
            [OperationKind.SpeedFramerate] = new(
                OperationKind.SpeedFramerate,
                "Speed/Framerate",
                typeof(SpeedFramerateRequest),
                OperationPhase.V11Plus,
                ["SpeedFactor > 0", "FPS opcional > 0"]),
            [OperationKind.SegmentHls] = new(
                OperationKind.SegmentHls,
                "HLS",
                typeof(SegmentHlsRequest),
                OperationPhase.V11Plus,
                ["Playlist/Segment pattern requeridos", "SegmentDuration > 0"])
        };

    public static OperationDescriptor Get(OperationKind kind) => Catalog[kind];

    public static bool TryParseLegacyOperation(string operation, out OperationKind kind)
    {
        kind = operation.Trim().ToLowerInvariant() switch
        {
            "trim" => OperationKind.Trim,
            "concat" => OperationKind.Concat,
            "extractaudio" or "extract_audio" or "splitaudio" => OperationKind.ExtractAudio,
            "extractvideo" or "extract_video" or "splitvideo" => OperationKind.ExtractVideo,
            "normalize" or "normalizeloudness" => OperationKind.NormalizeLoudness,
            "subtitle" or "subtitles" => OperationKind.Subtitle,
            "watermark" => OperationKind.WatermarkOverlay,
            "speed" or "speedframerate" => OperationKind.SpeedFramerate,
            "hls" or "segmenthls" => OperationKind.SegmentHls,
            _ => OperationKind.Convert
        };

        return true;
    }
}
