namespace VidEditor.Domain.Models;

public sealed record ToolchainCapabilitiesSnapshot(
    DateTimeOffset CapturedAt,
    ToolchainBinaryDiagnostic Ffmpeg,
    ToolchainBinaryDiagnostic Ffprobe,
    ToolchainBinaryDiagnostic? Ffplay,
    string FfmpegVersion,
    IReadOnlyList<string> SupportedVideoCodecs,
    IReadOnlyList<string> HardwareAccelerationMethods,
    IReadOnlyList<string> VideoEncoders,
    IReadOnlyList<string> AudioEncoders,
    IReadOnlyList<string> Muxers,
    IReadOnlyList<string> PixelFormats,
    IReadOnlyList<string>? SubtitleEncoders = null);
