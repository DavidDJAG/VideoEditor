namespace VideoEdit.Domain.Models;

public sealed class ToolPaths
{
    public string FfmpegPath { get; init; } = "ffmpeg";

    public string FfprobePath { get; init; } = "ffprobe";

    public bool UsingConfiguredDirectory { get; init; }

    public string DiagnosticSummary =>
        UsingConfiguredDirectory
            ? $"Usando carpeta configurada: {Path.GetDirectoryName(FfmpegPath)}"
            : "Usando ffmpeg/ffprobe desde PATH";
}
