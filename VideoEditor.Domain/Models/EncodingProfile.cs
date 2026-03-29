namespace VideoEditor.Domain.Models;

public sealed record EncodingProfile(
    string Name,
    string VideoCodec,
    string AudioCodec,
    string Container,
    string VideoBitrate,
    string AudioBitrate,
    string PixelFormat,
    string Preset)
{
    public ConvertOptions ToConvertOptions() => ConvertOptions.FromLegacyProfile(this);
}
