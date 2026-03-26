namespace VideoEdit.Domain.Models;

public sealed class MediaStreamInfo
{
    public int Index { get; init; }

    public string CodecType { get; init; } = string.Empty;

    public string CodecName { get; init; } = string.Empty;

    public string? CodecLongName { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public string? FrameRate { get; init; }

    public string? BitRate { get; init; }

    public string? PixelFormat { get; init; }

    public string? SampleRate { get; init; }

    public string? ChannelLayout { get; init; }
}
