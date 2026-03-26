namespace VideoEditor.Domain.Models;

public interface IFfmpegOperationRequest
{
    IReadOnlyList<string> Validate();
}

public abstract record FfmpegOperationRequest : IFfmpegOperationRequest
{
    public abstract IReadOnlyList<string> Validate();

    protected static void Require(bool condition, string message, List<string> errors)
    {
        if (!condition)
        {
            errors.Add(message);
        }
    }
}

public sealed record TrimRequest(string InputPath, string OutputPath, TimeSpan Start, TimeSpan End) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(Start >= TimeSpan.Zero, "Start must be non-negative.", errors);
        Require(End > Start, "End must be greater than Start.", errors);
        return errors;
    }
}

public sealed record ExtractAudioRequest(string InputPath, string OutputPath, string AudioCodec = "copy") : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(AudioCodec), "AudioCodec is required.", errors);
        return errors;
    }
}

public sealed record ExtractVideoRequest(string InputPath, string OutputPath, string VideoCodec = "copy") : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(VideoCodec), "VideoCodec is required.", errors);
        return errors;
    }
}

public sealed record ConvertRequest(string InputPath, string OutputPath, EncodingProfile EncodingProfile) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(EncodingProfile is not null, "EncodingProfile is required.", errors);
        return errors;
    }
}

public sealed record ConcatRequest(IReadOnlyList<string> Inputs, string OutputPath) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(Inputs is { Count: > 1 }, "At least two inputs are required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        return errors;
    }
}

public sealed record NormalizeLoudnessRequest(string InputPath, string OutputPath, double IntegratedLoudness = -16, double TruePeak = -1.5, double LoudnessRange = 11) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(IntegratedLoudness <= 0, "Integrated loudness should be <= 0 LUFS.", errors);
        Require(TruePeak <= 0, "TruePeak should be <= 0 dBTP.", errors);
        Require(LoudnessRange > 0, "LoudnessRange must be greater than 0.", errors);
        return errors;
    }
}

public enum SubtitleMode
{
    BurnIn,
    Mux
}

public sealed record SubtitleRequest(string InputPath, string SubtitlePath, string OutputPath, SubtitleMode Mode, string? Language = null) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(SubtitlePath), "SubtitlePath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        return errors;
    }
}

public sealed record ThumbnailContactSheetRequest(string InputPath, string OutputPatternOrPath, bool GenerateContactSheet, int Columns = 4, int Rows = 4, int FrameIntervalSeconds = 10) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPatternOrPath), "OutputPatternOrPath is required.", errors);
        Require(FrameIntervalSeconds > 0, "FrameIntervalSeconds must be greater than 0.", errors);
        if (GenerateContactSheet)
        {
            Require(Columns > 0, "Columns must be greater than 0.", errors);
            Require(Rows > 0, "Rows must be greater than 0.", errors);
        }

        return errors;
    }
}

public sealed record AudioChannelMapResampleRequest(string InputPath, string OutputPath, string ChannelLayout, int SampleRate, string AudioCodec = "aac") : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(ChannelLayout), "ChannelLayout is required.", errors);
        Require(SampleRate > 0, "SampleRate must be greater than 0.", errors);
        Require(!string.IsNullOrWhiteSpace(AudioCodec), "AudioCodec is required.", errors);
        return errors;
    }
}

public sealed record WatermarkOverlayRequest(string InputPath, string OutputPath, string? WatermarkImagePath, string? WatermarkText, string Position = "10:10") : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(Position), "Position is required.", errors);
        Require(!string.IsNullOrWhiteSpace(WatermarkImagePath) || !string.IsNullOrWhiteSpace(WatermarkText), "Provide WatermarkImagePath or WatermarkText.", errors);
        return errors;
    }
}

public sealed record SpeedFramerateRequest(string InputPath, string OutputPath, double SpeedFactor, double? OutputFrameRate = null) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(SpeedFactor > 0, "SpeedFactor must be greater than 0.", errors);
        if (OutputFrameRate.HasValue)
        {
            Require(OutputFrameRate.Value > 0, "OutputFrameRate must be greater than 0.", errors);
        }

        return errors;
    }
}

public sealed record SegmentHlsRequest(string InputPath, string OutputPlaylistPath, string SegmentFilePattern, int SegmentDurationSeconds = 6) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPlaylistPath), "OutputPlaylistPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(SegmentFilePattern), "SegmentFilePattern is required.", errors);
        Require(SegmentDurationSeconds > 0, "SegmentDurationSeconds must be greater than 0.", errors);
        return errors;
    }
}

public static class OperationValidation
{
    public static void ThrowIfInvalid(IFfmpegOperationRequest request)
    {
        var errors = request.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException($"Invalid operation request: {string.Join("; ", errors)}");
    }
}
