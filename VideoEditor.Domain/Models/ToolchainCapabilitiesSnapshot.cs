namespace VideoEditor.Domain.Models;

public sealed record ToolchainCapabilitiesSnapshot(
    DateTimeOffset CapturedAt,
    ToolchainBinaryDiagnostic Ffmpeg,
    ToolchainBinaryDiagnostic Ffprobe,
    ToolchainBinaryDiagnostic? Ffplay,
    string FfmpegVersion,
    IReadOnlyList<string> SupportedVideoCodecs,
    IReadOnlyList<string> HardwareAccelerationMethods);
