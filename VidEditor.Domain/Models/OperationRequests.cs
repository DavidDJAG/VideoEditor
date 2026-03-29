namespace VidEditor.Domain.Models;

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

public sealed record ConvertRequest(string InputPath, string OutputPath, ConvertOptions ConvertOptions) : FfmpegOperationRequest
{
    public ConvertRequest(string inputPath, string outputPath, EncodingProfile encodingProfile)
        : this(inputPath, outputPath, encodingProfile.ToConvertOptions())
    {
    }

    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(!string.IsNullOrWhiteSpace(InputPath), "InputPath is required.", errors);
        Require(!string.IsNullOrWhiteSpace(OutputPath), "OutputPath is required.", errors);
        Require(!string.Equals(InputPath, OutputPath, StringComparison.OrdinalIgnoreCase), "InputPath and OutputPath must be different.", errors);
        Require(ConvertOptions is not null, "ConvertOptions is required.", errors);

        if (ConvertOptions is null)
        {
            return errors;
        }

        ValidateConvertOptions(ConvertOptions.Normalize(), errors);
        return errors;
    }

    private static void ValidateConvertOptions(ConvertOptions options, List<string> errors)
    {
        Require(!string.IsNullOrWhiteSpace(options.Container), "Container is required.", errors);
        Require(ContainerCatalog.IsKnown(options.Container), $"Container '{options.Container}' is not supported.", errors);
        Require(options.Video.Mode != StreamProcessingMode.Disable || options.Audio.Mode != StreamProcessingMode.Disable, "At least one stream must remain enabled.", errors);

        ValidateVideo(options.Video, errors);
        ValidateAudio(options.Audio, errors);
        ValidateSubtitle(options.Subtitle ?? SubtitleOptions.Disabled(), options.Video, errors);
        ValidateMetadata(options.Metadata ?? MetadataOptions.CreateDefault(), errors);
    }

    private static void ValidateVideo(VideoEncodingOptions video, List<string> errors)
    {
        var cropRequested = video.CropX.HasValue || video.CropY.HasValue || video.CropWidth.HasValue || video.CropHeight.HasValue;

        if (video.Mode != StreamProcessingMode.Encode)
        {
            Require(video.PassMode == VideoPassMode.SinglePass, "Two-pass video encoding can only be enabled when video mode is Encode.", errors);
            Require(video.DeinterlaceMode == VideoDeinterlaceMode.Off, "Deinterlacing can only be enabled when video mode is Encode.", errors);
            Require(!cropRequested, "Crop can only be enabled when video mode is Encode.", errors);
            Require(!video.PadToSize, "Pad can only be enabled when video mode is Encode.", errors);
        }

        if (video.Mode == StreamProcessingMode.Disable)
        {
            return;
        }

        if (video.Mode == StreamProcessingMode.Copy)
        {
            return;
        }

        Require(!string.IsNullOrWhiteSpace(video.Codec), "Video codec is required when video encoding is enabled.", errors);

        switch (video.RateControlMode)
        {
            case VideoRateControlMode.Bitrate:
                Require(!string.IsNullOrWhiteSpace(video.Bitrate), "Video bitrate is required when rate control mode is Bitrate.", errors);
                break;
            case VideoRateControlMode.ConstantQuality:
                Require(video.Crf.HasValue, "CRF is required when rate control mode is ConstantQuality.", errors);
                if (video.Crf.HasValue)
                {
                    Require(video.Crf.Value is >= 0 and <= 63, "CRF must be between 0 and 63.", errors);
                }
                break;
        }

        if (video.FrameRateMode == FrameRateMode.SetOutput)
        {
            Require(video.FrameRate.HasValue && video.FrameRate.Value > 0, "FrameRate must be greater than 0 when output frame rate is set.", errors);
        }

        if (video.ScaleMode == ScaleMode.SetOutput)
        {
            Require((video.Width.HasValue && video.Width.Value > 0) || (video.Height.HasValue && video.Height.Value > 0), "Width or Height must be greater than 0 when scaling is enabled.", errors);
            if (video.Width.HasValue)
            {
                Require(video.Width.Value > 0, "Width must be greater than 0 when provided.", errors);
            }

            if (video.Height.HasValue)
            {
                Require(video.Height.Value > 0, "Height must be greater than 0 when provided.", errors);
            }
        }

        if (video.GopSize.HasValue)
        {
            Require(video.GopSize.Value > 0, "GopSize must be greater than 0 when provided.", errors);
        }

        if (video.SourceStreamIndex.HasValue)
        {
            Require(video.SourceStreamIndex.Value >= 0, "Video source stream index must be greater than or equal to 0 when provided.", errors);
        }

        if (video.PassMode == VideoPassMode.TwoPass)
        {
            Require(video.RateControlMode == VideoRateControlMode.Bitrate, "Two-pass video encoding requires rate control mode Bitrate.", errors);
            Require(!string.IsNullOrWhiteSpace(video.Bitrate), "Two-pass video encoding requires a target video bitrate.", errors);
        }

        if (video.DeinterlaceMode != VideoDeinterlaceMode.Off)
        {
            Require(video.Mode == StreamProcessingMode.Encode, "Deinterlacing can only be enabled when video mode is Encode.", errors);
        }

        if (cropRequested)
        {
            Require(video.Mode == StreamProcessingMode.Encode, "Crop can only be enabled when video mode is Encode.", errors);
            Require(video.CropWidth.HasValue && video.CropWidth.Value > 0, "CropWidth must be greater than 0 when crop is enabled.", errors);
            Require(video.CropHeight.HasValue && video.CropHeight.Value > 0, "CropHeight must be greater than 0 when crop is enabled.", errors);
            if (video.CropX.HasValue)
            {
                Require(video.CropX.Value >= 0, "CropX must be greater than or equal to 0 when provided.", errors);
            }

            if (video.CropY.HasValue)
            {
                Require(video.CropY.Value >= 0, "CropY must be greater than or equal to 0 when provided.", errors);
            }
        }

        if (video.PadToSize)
        {
            Require(video.Mode == StreamProcessingMode.Encode, "Pad can only be enabled when video mode is Encode.", errors);
            Require(video.PadWidth.HasValue && video.PadWidth.Value > 0, "PadWidth must be greater than 0 when pad is enabled.", errors);
            Require(video.PadHeight.HasValue && video.PadHeight.Value > 0, "PadHeight must be greater than 0 when pad is enabled.", errors);
            if (video.PadX.HasValue)
            {
                Require(video.PadX.Value >= 0, "PadX must be greater than or equal to 0 when provided.", errors);
            }

            if (video.PadY.HasValue)
            {
                Require(video.PadY.Value >= 0, "PadY must be greater than or equal to 0 when provided.", errors);
            }

            if (video.ScaleMode == ScaleMode.SetOutput)
            {
                if (video.Width.HasValue && video.PadWidth.HasValue)
                {
                    Require(video.PadWidth.Value >= video.Width.Value, "PadWidth must be greater than or equal to the scaled width.", errors);
                }

                if (video.Height.HasValue && video.PadHeight.HasValue)
                {
                    Require(video.PadHeight.Value >= video.Height.Value, "PadHeight must be greater than or equal to the scaled height.", errors);
                }
            }
        }
    }

    private static void ValidateAudio(AudioEncodingOptions audio, List<string> errors)
    {
        if (audio.Mode != StreamProcessingMode.Encode)
        {
            Require(audio.NormalizationMode == AudioNormalizationMode.None, "Audio normalization requires audio mode Encode.", errors);
        }

        if (audio.SourceStreamIndex.HasValue)
        {
            Require(audio.SourceStreamIndex.Value >= 0, "Audio source stream index must be greater than or equal to 0 when provided.", errors);
        }

        ValidateAdditionalStreamIndexes(audio.AdditionalSourceStreamIndexes, "Audio additional source stream index", errors);

        if (audio.Mode == StreamProcessingMode.Disable)
        {
            return;
        }

        if (audio.Mode == StreamProcessingMode.Copy)
        {
            return;
        }

        Require(!string.IsNullOrWhiteSpace(audio.Codec), "Audio codec is required when audio encoding is enabled.", errors);

        if (audio.SampleRate.HasValue)
        {
            Require(audio.SampleRate.Value > 0, "SampleRate must be greater than 0 when provided.", errors);
        }

        if (audio.Channels.HasValue)
        {
            Require(audio.Channels.Value > 0, "Channels must be greater than 0 when provided.", errors);
        }

        if (audio.NormalizationMode != AudioNormalizationMode.None)
        {
            Require(audio.Mode == StreamProcessingMode.Encode, "Audio normalization requires audio mode Encode.", errors);
        }

        if (audio.NormalizationMode == AudioNormalizationMode.Loudnorm)
        {
            if (audio.LoudnessTarget.HasValue)
            {
                Require(audio.LoudnessTarget.Value <= 0, "LoudnessTarget should be <= 0 LUFS when provided.", errors);
            }

            if (audio.TruePeak.HasValue)
            {
                Require(audio.TruePeak.Value <= 0, "TruePeak should be <= 0 dBTP when provided.", errors);
            }

            if (audio.LoudnessRange.HasValue)
            {
                Require(audio.LoudnessRange.Value > 0, "LoudnessRange must be greater than 0 when provided.", errors);
            }
        }
    }

    private static void ValidateSubtitle(SubtitleOptions subtitle, VideoEncodingOptions video, List<string> errors)
    {
        if (subtitle.SourceStreamIndex.HasValue)
        {
            Require(subtitle.SourceStreamIndex.Value >= 0, "Subtitle source stream index must be greater than or equal to 0 when provided.", errors);
        }

        ValidateAdditionalStreamIndexes(subtitle.AdditionalSourceStreamIndexes, "Subtitle additional source stream index", errors);

        if (subtitle.Mode == SubtitleProcessingMode.Encode)
        {
            Require(!string.IsNullOrWhiteSpace(subtitle.Codec), "Subtitle codec is required when subtitle mode is Encode.", errors);
        }

        if (subtitle.Mode == SubtitleProcessingMode.BurnIn)
        {
            Require(video.Mode == StreamProcessingMode.Encode, "Burn-in subtitles require video mode Encode.", errors);
        }
    }

    private static void ValidateAdditionalStreamIndexes(IReadOnlyList<int>? indexes, string label, List<string> errors)
    {
        if (indexes is null)
        {
            return;
        }

        foreach (var index in indexes)
        {
            Require(index >= 0, $"{label} must be greater than or equal to 0 when provided.", errors);
        }
    }

    private static void ValidateMetadata(MetadataOptions metadata, List<string> errors)
    {
        _ = metadata;
    }
}

public sealed record ConcatRequest(IReadOnlyList<string> Inputs, string OutputPath, string? ManifestPath = null) : FfmpegOperationRequest
{
    public override IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Require(Inputs is { Count: > 1 }, "At least two inputs are required.", errors);
        Require(Inputs is not null && Inputs.All(input => !string.IsNullOrWhiteSpace(input)), "Concat inputs must be a non-empty ordered list.", errors);
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
