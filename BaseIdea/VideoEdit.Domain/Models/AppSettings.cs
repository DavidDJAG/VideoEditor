namespace VideoEdit.Domain.Models;

public sealed class AppSettings
{
    public string? FfmpegDirectory { get; set; }

    public string? LastInputDirectory { get; set; }

    public string? LastOutputDirectory { get; set; }

    public bool ShowCommandPreview { get; set; } = true;
}
