namespace VideoEditor.Domain.Models;

public sealed record ConvertPresetRecord(string Name, ConvertOptions Options, DateTimeOffset SavedAtUtc)
{
    public ConvertPresetRecord Normalize()
        => this with
        {
            Name = string.IsNullOrWhiteSpace(Name) ? string.Empty : Name.Trim(),
            Options = Options is null ? ConvertOptions.CreateBalancedMp4H264() : Options.Normalize()
        };
}
