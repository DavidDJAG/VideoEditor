using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Services;

public sealed class CommandBuilder : ICommandBuilder
{
    public string Build(IFfmpegOperationRequest request)
    {
        OperationValidation.ThrowIfInvalid(request);

        var arguments = request switch
        {
            TrimRequest trim => BuildTrim(trim),
            ExtractAudioRequest extractAudio => BuildExtractAudio(extractAudio),
            ExtractVideoRequest extractVideo => BuildExtractVideo(extractVideo),
            ConvertRequest convert => BuildConvert(convert),
            ConcatRequest concat => BuildConcat(concat),
            NormalizeLoudnessRequest normalize => BuildNormalize(normalize),
            SubtitleRequest subtitle => BuildSubtitle(subtitle),
            ThumbnailContactSheetRequest thumbnail => BuildThumbnail(thumbnail),
            AudioChannelMapResampleRequest audioMap => BuildAudioMap(audioMap),
            WatermarkOverlayRequest watermark => BuildWatermark(watermark),
            SpeedFramerateRequest speed => BuildSpeed(speed),
            SegmentHlsRequest hls => BuildSegmentHls(hls),
            _ => throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}")
        };

        return string.Join(' ', arguments);
    }

    public string BuildTrim(OperationParameters parameters)
    {
        var output = parameters.OutputPath ?? throw new InvalidOperationException("Trim requires OutputPath.");
        var start = parameters.Start ?? TimeSpan.Zero;
        var end = parameters.End ?? throw new InvalidOperationException("Trim requires End time.");
        return Build(new TrimRequest(parameters.InputPath, output, start, end));
    }

    public string BuildTranscode(OperationParameters parameters)
    {
        var output = parameters.OutputPath ?? throw new InvalidOperationException("Transcode requires OutputPath.");
        var profile = parameters.EncodingProfile ?? throw new InvalidOperationException("Transcode requires EncodingProfile.");
        return Build(new ConvertRequest(parameters.InputPath, output, profile));
    }

    public string BuildConcat(OperationParameters parameters)
    {
        var output = parameters.OutputPath ?? throw new InvalidOperationException("Concat requires OutputPath.");
        var inputs = parameters.ConcatInputs ?? throw new InvalidOperationException("Concat requires ConcatInputs.");
        return Build(new ConcatRequest(inputs, output));
    }

    public string BuildProbe(string inputPath)
    {
        var args = new List<string>
        {
            "-v", "quiet",
            "-print_format", "json",
            "-show_format",
            "-show_streams",
            Quote(inputPath)
        };

        return string.Join(' ', args);
    }

    private static IReadOnlyList<string> BuildTrim(TrimRequest request)
    {
        var duration = request.End - request.Start;
        return new List<string>
        {
            "-y",
            "-ss", request.Start.ToString("c"),
            "-i", Quote(request.InputPath),
            "-t", duration.ToString("c"),
            "-c", "copy",
            Quote(request.OutputPath)
        };
    }

    private static IReadOnlyList<string> BuildExtractAudio(ExtractAudioRequest request)
        => new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-vn",
            "-c:a", request.AudioCodec,
            Quote(request.OutputPath)
        };

    private static IReadOnlyList<string> BuildExtractVideo(ExtractVideoRequest request)
        => new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-an",
            "-c:v", request.VideoCodec,
            Quote(request.OutputPath)
        };

    private static IReadOnlyList<string> BuildConvert(ConvertRequest request)
        => new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-c:v", request.EncodingProfile.VideoCodec,
            "-b:v", request.EncodingProfile.VideoBitrate,
            "-preset", request.EncodingProfile.Preset,
            "-pix_fmt", request.EncodingProfile.PixelFormat,
            "-c:a", request.EncodingProfile.AudioCodec,
            "-b:a", request.EncodingProfile.AudioBitrate,
            Quote(request.OutputPath)
        };

    private static IReadOnlyList<string> BuildConcat(ConcatRequest request)
    {
        var concatInput = $"concat:{string.Join('|', request.Inputs)}";
        return new List<string>
        {
            "-y",
            "-i", Quote(concatInput),
            "-c", "copy",
            Quote(request.OutputPath)
        };
    }

    private static IReadOnlyList<string> BuildNormalize(NormalizeLoudnessRequest request)
    {
        var filter = $"loudnorm=I={request.IntegratedLoudness}:TP={request.TruePeak}:LRA={request.LoudnessRange}";
        return new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-af", Quote(filter),
            "-c:v", "copy",
            Quote(request.OutputPath)
        };
    }

    private static IReadOnlyList<string> BuildSubtitle(SubtitleRequest request)
    {
        if (request.Mode == SubtitleMode.BurnIn)
        {
            var filter = $"subtitles={EscapeFilterPath(request.SubtitlePath)}";
            return new List<string>
            {
                "-y",
                "-i", Quote(request.InputPath),
                "-vf", Quote(filter),
                "-c:a", "copy",
                Quote(request.OutputPath)
            };
        }

        var trackLanguage = string.IsNullOrWhiteSpace(request.Language) ? "und" : request.Language;
        return new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-i", Quote(request.SubtitlePath),
            "-c", "copy",
            "-c:s", "mov_text",
            "-metadata:s:s:0", Quote($"language={trackLanguage}"),
            Quote(request.OutputPath)
        };
    }

    private static IReadOnlyList<string> BuildThumbnail(ThumbnailContactSheetRequest request)
    {
        var filter = request.GenerateContactSheet
            ? $"fps=1/{request.FrameIntervalSeconds},scale=320:-1,tile={request.Columns}x{request.Rows}"
            : $"fps=1/{request.FrameIntervalSeconds}";

        return new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-vf", Quote(filter),
            Quote(request.OutputPatternOrPath)
        };
    }

    private static IReadOnlyList<string> BuildAudioMap(AudioChannelMapResampleRequest request)
        => new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-af", Quote($"aresample={request.SampleRate}"),
            "-ac", ChannelCountFromLayout(request.ChannelLayout).ToString(),
            "-c:a", request.AudioCodec,
            "-vn",
            Quote(request.OutputPath)
        };

    private static IReadOnlyList<string> BuildWatermark(WatermarkOverlayRequest request)
    {
        var args = new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath)
        };

        if (!string.IsNullOrWhiteSpace(request.WatermarkImagePath))
        {
            args.AddRange(new[]
            {
                "-i", Quote(request.WatermarkImagePath),
                "-filter_complex", Quote($"overlay={request.Position}")
            });
        }
        else
        {
            var text = EscapeDrawText(request.WatermarkText!);
            args.AddRange(new[]
            {
                "-vf", Quote($"drawtext=text='{text}':x={request.Position.Split(':')[0]}:y={request.Position.Split(':')[1]}")
            });
        }

        args.AddRange(new[] { "-c:a", "copy", Quote(request.OutputPath) });
        return args;
    }

    private static IReadOnlyList<string> BuildSpeed(SpeedFramerateRequest request)
    {
        var videoFilter = $"setpts={1 / request.SpeedFactor:0.######}*PTS";
        if (request.OutputFrameRate.HasValue)
        {
            videoFilter += $",fps={request.OutputFrameRate.Value:0.###}";
        }

        var atempo = request.SpeedFactor is >= 0.5 and <= 2.0
            ? request.SpeedFactor.ToString("0.###")
            : "2.0";

        return new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-filter:v", Quote(videoFilter),
            "-filter:a", Quote($"atempo={atempo}"),
            Quote(request.OutputPath)
        };
    }

    private static IReadOnlyList<string> BuildSegmentHls(SegmentHlsRequest request)
        => new List<string>
        {
            "-y",
            "-i", Quote(request.InputPath),
            "-c:v", "libx264",
            "-c:a", "aac",
            "-f", "hls",
            "-hls_time", request.SegmentDurationSeconds.ToString(),
            "-hls_playlist_type", "vod",
            "-hls_segment_filename", Quote(request.SegmentFilePattern),
            Quote(request.OutputPlaylistPath)
        };

    private static string Quote(string value) => $"\"{value}\"";

    private static string EscapeFilterPath(string value)
        => value.Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'");

    private static string EscapeDrawText(string value)
        => value.Replace("\\", "\\\\").Replace("'", "\\'").Replace(":", "\\:");

    private static int ChannelCountFromLayout(string layout)
        => layout.ToLowerInvariant() switch
        {
            "mono" => 1,
            "stereo" => 2,
            "2.1" => 3,
            "5.1" => 6,
            "7.1" => 8,
            _ => 2
        };
}
