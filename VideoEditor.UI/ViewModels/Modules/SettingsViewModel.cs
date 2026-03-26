using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class SettingsViewModel
{
    public ToolPaths ToolPaths { get; set; } = new("ffmpeg", "ffprobe", "ffplay");

    public EncodingProfile DefaultProfile { get; set; } =
        new("Balanced", "libx264", "aac", "mp4", "4M", "192k", "yuv420p", "medium");
}
