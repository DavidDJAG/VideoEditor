namespace VideoEdit.Domain.Models;

public sealed class MediaJobRequest
{
    public required MediaJobType JobType { get; init; }

    public List<string> InputPaths { get; init; } = [];

    public string OutputPath { get; set; } = string.Empty;

    public TimeRange? TimeRange { get; init; }

    public bool AccurateSeek { get; init; }

    public bool OverwriteOutput { get; init; } = true;

    public string? VideoCodec { get; init; }

    public string? AudioCodec { get; init; }

    public string? Crf { get; init; }

    public string? Preset { get; init; }

    public string? AudioBitrate { get; init; }

    public string? PixelFormat { get; init; }

    public string? ContainerExtension { get; init; }

    public bool VideoCopy { get; init; }

    public bool AudioCopy { get; init; }

    public string? SelectedPresetName { get; init; }
}
