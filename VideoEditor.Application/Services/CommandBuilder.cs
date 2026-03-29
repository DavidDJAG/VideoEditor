using System.Globalization;
using System.IO;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Services;

public sealed class CommandBuilder : ICommandBuilder
{
    public string Build(IFfmpegOperationRequest request)
    {
        var commands = BuildCommandSequence(request);
        return commands.Count == 1
            ? commands[0]
            : string.Join($"{Environment.NewLine}{Environment.NewLine}", commands.Select((command, index) => $"Pass {index + 1}:{Environment.NewLine}{command}"));
    }

    public IReadOnlyList<string> BuildCommandSequence(IFfmpegOperationRequest request)
    {
        OperationValidation.ThrowIfInvalid(request);

        if (request is ConvertRequest convert)
        {
            return BuildConvertCommandSequence(convert)
                .Select(static arguments => string.Join(' ', arguments))
                .ToArray();
        }

        return [string.Join(' ', BuildSingleCommandArguments(request))];
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
        var options = parameters.ConvertOptions
            ?? parameters.EncodingProfile?.ToConvertOptions()
            ?? throw new InvalidOperationException("Transcode requires ConvertOptions or EncodingProfile.");

        return Build(new ConvertRequest(parameters.InputPath, output, options));
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

    public static string GetTwoPassLogFilePrefix(string outputPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        var fileName = Path.GetFileNameWithoutExtension(outputPath);
        return Path.Combine(string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory!, $"{fileName}.ffmpeg2pass");
    }

    public static IReadOnlyList<string> GetTwoPassLogArtifacts(string outputPath)
    {
        var prefix = GetTwoPassLogFilePrefix(outputPath);
        var directory = Path.GetDirectoryName(prefix);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        var fileName = Path.GetFileName(prefix);
        return Directory.GetFiles(directory, $"{fileName}*");
    }

    private static IReadOnlyList<string> BuildSingleCommandArguments(IFfmpegOperationRequest request)
        => request switch
        {
            TrimRequest trim => BuildTrim(trim),
            ExtractAudioRequest extractAudio => BuildExtractAudio(extractAudio),
            ExtractVideoRequest extractVideo => BuildExtractVideo(extractVideo),
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

    private static IReadOnlyList<IReadOnlyList<string>> BuildConvertCommandSequence(ConvertRequest request)
    {
        var options = request.ConvertOptions.Normalize();
        if (ShouldUseTwoPass(options.Video))
        {
            var passLogFilePrefix = GetTwoPassLogFilePrefix(request.OutputPath);
            return new[]
            {
                BuildConvertPass(request, options, 1, passLogFilePrefix),
                BuildConvertPass(request, options, 2, passLogFilePrefix)
            };
        }

        return new[] { BuildConvertSinglePass(request, options) };
    }

    private static bool ShouldUseTwoPass(VideoEncodingOptions video)
        => video.Mode == StreamProcessingMode.Encode
           && video.PassMode == VideoPassMode.TwoPass
           && video.RateControlMode == VideoRateControlMode.Bitrate
           && !string.IsNullOrWhiteSpace(video.Bitrate);

    private static IReadOnlyList<string> BuildConvertSinglePass(ConvertRequest request, ConvertOptions options)
    {
        var args = CreateConvertBaseArgs(request, options);
        BuildConvertMapArgs(args, options);
        BuildConvertVideoArgs(args, request, options);
        BuildConvertAudioArgs(args, options.Audio);
        BuildConvertSubtitleArgs(args, options);
        BuildConvertAudioFilterArgs(args, options.Audio);
        BuildConvertMuxArgs(args, options);
        args.Add(Quote(request.OutputPath));
        return args;
    }

    private static IReadOnlyList<string> BuildConvertPass(ConvertRequest request, ConvertOptions options, int passNumber, string passLogFilePrefix)
    {
        var args = CreateConvertBaseArgs(request, options);
        BuildConvertMapArgs(args, options, includeSubtitleCopy: false);
        BuildConvertVideoArgs(args, request, options, passNumber, passLogFilePrefix);

        if (passNumber == 1)
        {
            args.Add("-an");
            args.AddRange(["-f", "null", GetNullOutputTarget()]);
            return args;
        }

        BuildConvertAudioArgs(args, options.Audio);
        BuildConvertSubtitleArgs(args, options);
        BuildConvertAudioFilterArgs(args, options.Audio);
        BuildConvertMuxArgs(args, options);
        args.Add(Quote(request.OutputPath));
        return args;
    }

    private static List<string> CreateConvertBaseArgs(ConvertRequest request, ConvertOptions options)
    {
        var args = new List<string>
        {
            options.OverwriteMode == OverwriteMode.Overwrite ? "-y" : "-n"
        };

        if (options.UseHardwareAcceleration)
        {
            args.AddRange(["-hwaccel", "auto"]);
        }

        args.AddRange(["-i", Quote(request.InputPath)]);
        return args;
    }

    private static void BuildConvertVideoArgs(List<string> args, ConvertRequest request, ConvertOptions options, int? passNumber = null, string? passLogFilePrefix = null)
    {
        var video = options.Video;
        switch (video.Mode)
        {
            case StreamProcessingMode.Disable:
                args.Add("-vn");
                return;
            case StreamProcessingMode.Copy:
                args.AddRange(["-c:v", "copy"]);
                return;
        }

        args.AddRange(["-c:v", video.Codec]);

        switch (video.RateControlMode)
        {
            case VideoRateControlMode.ConstantQuality when video.Crf.HasValue:
                args.AddRange(["-crf", video.Crf.Value.ToString(CultureInfo.InvariantCulture)]);
                break;
            case VideoRateControlMode.Bitrate when !string.IsNullOrWhiteSpace(video.Bitrate):
                args.AddRange(["-b:v", video.Bitrate!]);
                break;
        }

        if (!string.IsNullOrWhiteSpace(video.Preset))
        {
            args.AddRange(["-preset", video.Preset!]);
        }

        if (!string.IsNullOrWhiteSpace(video.Tune))
        {
            args.AddRange(["-tune", video.Tune!]);
        }

        if (!string.IsNullOrWhiteSpace(video.PixelFormat))
        {
            args.AddRange(["-pix_fmt", video.PixelFormat!]);
        }

        if (passNumber.HasValue && !string.IsNullOrWhiteSpace(passLogFilePrefix))
        {
            args.AddRange(["-pass", passNumber.Value.ToString(CultureInfo.InvariantCulture)]);
            args.AddRange(["-passlogfile", Quote(passLogFilePrefix)]);
        }

        var filters = BuildVideoFilters(video, options.Subtitle ?? SubtitleOptions.Disabled(), request.InputPath);
        if (filters.Count > 0)
        {
            args.AddRange(["-vf", Quote(string.Join(',', filters))]);
        }

        if (video.FrameRateMode == FrameRateMode.SetOutput && video.FrameRate.HasValue)
        {
            args.AddRange(["-r", FormatDecimal(video.FrameRate.Value)]);
        }

        if (!string.IsNullOrWhiteSpace(video.Profile))
        {
            args.AddRange(["-profile:v", video.Profile!]);
        }

        if (!string.IsNullOrWhiteSpace(video.Level))
        {
            args.AddRange(["-level:v", video.Level!]);
        }

        if (video.GopSize.HasValue)
        {
            args.AddRange(["-g", video.GopSize.Value.ToString(CultureInfo.InvariantCulture)]);
        }
    }

    private static List<string> BuildVideoFilters(VideoEncodingOptions video, SubtitleOptions subtitle, string inputPath)
    {
        var filters = new List<string>();

        if (video.DeinterlaceMode == VideoDeinterlaceMode.Yadif)
        {
            filters.Add("yadif");
        }

        if (video.CropWidth.HasValue && video.CropHeight.HasValue)
        {
            filters.Add(BuildCropFilter(video.CropWidth.Value, video.CropHeight.Value, video.CropX, video.CropY));
        }

        if (video.ScaleMode == ScaleMode.SetOutput)
        {
            filters.Add(BuildScaleFilter(video.Width, video.Height));
        }

        if (video.PadToSize && video.PadWidth.HasValue && video.PadHeight.HasValue)
        {
            filters.Add(BuildPadFilter(video.PadWidth.Value, video.PadHeight.Value, video.PadX, video.PadY));
        }

        if (subtitle.Mode == SubtitleProcessingMode.BurnIn)
        {
            var subtitleIndex = subtitle.SourceStreamIndex.GetValueOrDefault();
            filters.Add($"subtitles={EscapeFilterPath(inputPath)}:si={subtitleIndex.ToString(CultureInfo.InvariantCulture)}");
        }

        return filters;
    }

    private static string BuildCropFilter(int width, int height, int? x, int? y)
        => $"crop={width}:{height}:{(x ?? 0).ToString(CultureInfo.InvariantCulture)}:{(y ?? 0).ToString(CultureInfo.InvariantCulture)}";

    private static string BuildScaleFilter(int? width, int? height)
    {
        var widthToken = width.HasValue && width.Value > 0
            ? width.Value.ToString(CultureInfo.InvariantCulture)
            : "-2";
        var heightToken = height.HasValue && height.Value > 0
            ? height.Value.ToString(CultureInfo.InvariantCulture)
            : "-2";

        return $"scale={widthToken}:{heightToken}";
    }

    private static string BuildPadFilter(int width, int height, int? x, int? y)
    {
        var xToken = x.HasValue ? x.Value.ToString(CultureInfo.InvariantCulture) : "(ow-iw)/2";
        var yToken = y.HasValue ? y.Value.ToString(CultureInfo.InvariantCulture) : "(oh-ih)/2";
        return $"pad={width}:{height}:{xToken}:{yToken}:black";
    }

    private static void BuildConvertMapArgs(List<string> args, ConvertOptions options, bool includeSubtitleCopy = true)
    {
        var subtitle = options.Subtitle ?? SubtitleOptions.Disabled();
        var audioMaps = options.Audio.GetSelectedStreamIndexes();
        var subtitleMaps = subtitle.GetSelectedStreamIndexes();
        var mapSubtitleStreams = includeSubtitleCopy && subtitle.Mode is SubtitleProcessingMode.Copy or SubtitleProcessingMode.Encode;
        var useExplicitMapping = options.Video.SourceStreamIndex.HasValue
            || audioMaps.Count > 0
            || (mapSubtitleStreams && subtitleMaps.Count > 0);

        if (!useExplicitMapping)
        {
            return;
        }

        if (options.Video.Mode != StreamProcessingMode.Disable)
        {
            args.AddRange(["-map", BuildStreamMapSpecifier('v', options.Video.SourceStreamIndex)]);
        }

        if (options.Audio.Mode != StreamProcessingMode.Disable)
        {
            foreach (var mapSpecifier in BuildStreamMapSpecifiers('a', options.Audio.SourceStreamIndex, options.Audio.AdditionalSourceStreamIndexes))
            {
                args.AddRange(["-map", mapSpecifier]);
            }
        }

        if (mapSubtitleStreams)
        {
            foreach (var mapSpecifier in BuildStreamMapSpecifiers('s', subtitle.SourceStreamIndex, subtitle.AdditionalSourceStreamIndexes))
            {
                args.AddRange(["-map", mapSpecifier]);
            }
        }
    }

    private static IEnumerable<string> BuildStreamMapSpecifiers(char streamType, int? primaryIndex, IReadOnlyList<int>? additionalIndexes)
    {
        var indexes = NormalizeStreamIndexCollection(primaryIndex, additionalIndexes);
        if (indexes.Count == 0)
        {
            yield return BuildStreamMapSpecifier(streamType, primaryIndex);
            yield break;
        }

        foreach (var index in indexes)
        {
            yield return BuildStreamMapSpecifier(streamType, index);
        }
    }

    private static IReadOnlyList<int> NormalizeStreamIndexCollection(int? primaryIndex, IReadOnlyList<int>? additionalIndexes)
    {
        var values = new List<int>();

        if (primaryIndex is >= 0)
        {
            values.Add(primaryIndex.Value);
        }

        if (additionalIndexes is not null)
        {
            foreach (var index in additionalIndexes.Where(static value => value >= 0))
            {
                if (!values.Contains(index))
                {
                    values.Add(index);
                }
            }
        }

        return values;
    }

    private static string BuildStreamMapSpecifier(char streamType, int? typeIndex)
        => $"0:{streamType}:{typeIndex.GetValueOrDefault()}?";

    private static void BuildConvertAudioArgs(List<string> args, AudioEncodingOptions audio)
    {
        switch (audio.Mode)
        {
            case StreamProcessingMode.Disable:
                args.Add("-an");
                return;
            case StreamProcessingMode.Copy:
                args.AddRange(["-c:a", "copy"]);
                return;
        }

        args.AddRange(["-c:a", audio.Codec]);

        if (!string.IsNullOrWhiteSpace(audio.Bitrate))
        {
            args.AddRange(["-b:a", audio.Bitrate!]);
        }

        if (audio.SampleRate.HasValue)
        {
            args.AddRange(["-ar", audio.SampleRate.Value.ToString(CultureInfo.InvariantCulture)]);
        }

        if (audio.Channels.HasValue)
        {
            args.AddRange(["-ac", audio.Channels.Value.ToString(CultureInfo.InvariantCulture)]);
        }

        if (!string.IsNullOrWhiteSpace(audio.ChannelLayout))
        {
            args.AddRange(["-channel_layout", audio.ChannelLayout!]);
        }
    }

    private static void BuildConvertSubtitleArgs(List<string> args, ConvertOptions options)
    {
        var subtitle = options.Subtitle ?? SubtitleOptions.Disabled();
        switch (subtitle.Mode)
        {
            case SubtitleProcessingMode.Disable:
                args.Add("-sn");
                return;
            case SubtitleProcessingMode.Copy:
                args.AddRange(["-c:s", "copy"]);
                ApplySubtitleMetadata(args, subtitle, BuildMappedSubtitleStreamCount(subtitle));
                return;
            case SubtitleProcessingMode.Encode:
                args.AddRange(["-c:s", ResolveSubtitleCodec(subtitle, options.Container)]);
                ApplySubtitleMetadata(args, subtitle, BuildMappedSubtitleStreamCount(subtitle));
                return;
            case SubtitleProcessingMode.BurnIn:
                args.Add("-sn");
                return;
        }
    }

    private static int BuildMappedSubtitleStreamCount(SubtitleOptions subtitle)
    {
        var count = subtitle.GetSelectedStreamIndexes().Count;
        return count > 0 ? count : 1;
    }

    private static void ApplySubtitleMetadata(List<string> args, SubtitleOptions subtitle, int mappedStreamCount)
    {
        if (!string.IsNullOrWhiteSpace(subtitle.Language))
        {
            for (var index = 0; index < mappedStreamCount; index++)
            {
                args.AddRange([$"-metadata:s:s:{index}", Quote($"language={subtitle.Language}")]);
            }
        }

        if (subtitle.SetAsDefault)
        {
            args.AddRange(["-disposition:s:0", "default"]);
        }
    }

    private static string ResolveSubtitleCodec(SubtitleOptions subtitle, string container)
    {
        if (!string.IsNullOrWhiteSpace(subtitle.Codec))
        {
            return subtitle.Codec!;
        }

        return NormalizeContainer(container) switch
        {
            "mp4" or "mov" or "m4v" => "mov_text",
            "webm" => "webvtt",
            _ => "srt"
        };
    }

    private static void BuildConvertAudioFilterArgs(List<string> args, AudioEncodingOptions audio)
    {
        if (audio.Mode != StreamProcessingMode.Encode)
        {
            return;
        }

        var audioFilter = BuildAudioFilter(audio);
        if (!string.IsNullOrWhiteSpace(audioFilter))
        {
            args.AddRange(["-af", Quote(audioFilter)]);
        }
    }

    private static string? BuildAudioFilter(AudioEncodingOptions audio)
        => audio.NormalizationMode switch
        {
            AudioNormalizationMode.Loudnorm => $"loudnorm=I={FormatDecimal(audio.LoudnessTarget ?? -16)}:TP={FormatDecimal(audio.TruePeak ?? -1.5)}:LRA={FormatDecimal(audio.LoudnessRange ?? 11)}",
            AudioNormalizationMode.Dynaudnorm => "dynaudnorm",
            _ => null
        };

    private static void BuildConvertMuxArgs(List<string> args, ConvertOptions options)
    {
        var metadata = options.Metadata ?? MetadataOptions.CreateDefault();

        args.AddRange(["-map_metadata", metadata.CopyMetadata ? "0" : "-1"]);
        args.AddRange(["-map_chapters", metadata.CopyChapters ? "0" : "-1"]);

        if (!string.IsNullOrWhiteSpace(metadata.Title))
        {
            args.AddRange(["-metadata", Quote($"title={metadata.Title}")]);
        }

        if (!string.IsNullOrWhiteSpace(metadata.Artist))
        {
            args.AddRange(["-metadata", Quote($"artist={metadata.Artist}")]);
        }

        if (!string.IsNullOrWhiteSpace(metadata.Comment))
        {
            args.AddRange(["-metadata", Quote($"comment={metadata.Comment}")]);
        }

        if (options.FastStart && SupportsFastStart(options.Container))
        {
            args.AddRange(["-movflags", "+faststart"]);
        }

        if (!string.IsNullOrWhiteSpace(options.Container))
        {
            args.AddRange(["-f", options.Container]);
        }
    }

    private static bool SupportsFastStart(string container)
        => container is "mp4" or "mov" or "m4v";

    private static string NormalizeContainer(string container)
        => string.IsNullOrWhiteSpace(container)
            ? string.Empty
            : container.Trim().TrimStart('.').ToLowerInvariant();

    private static string GetNullOutputTarget()
        => OperatingSystem.IsWindows() ? "NUL" : "/dev/null";

    private static IReadOnlyList<string> BuildConcat(ConcatRequest request)
        => new List<string>
        {
            "-y",
            "-f", "concat",
            "-safe", "0",
            "-i", Quote(request.ManifestPath ?? GetConcatManifestPath(request.OutputPath)),
            "-c", "copy",
            Quote(request.OutputPath)
        };

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
            "-ac", ChannelCountFromLayout(request.ChannelLayout).ToString(CultureInfo.InvariantCulture),
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

        args.AddRange(["-c:a", "copy", Quote(request.OutputPath)]);
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
            ? request.SpeedFactor.ToString("0.###", CultureInfo.InvariantCulture)
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
            "-hls_time", request.SegmentDurationSeconds.ToString(CultureInfo.InvariantCulture),
            "-hls_playlist_type", "vod",
            "-hls_segment_filename", Quote(request.SegmentFilePattern),
            Quote(request.OutputPlaylistPath)
        };

    private static string FormatDecimal(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Quote(string value) => $"\"{value}\"";

    public static string GetConcatManifestPath(string outputPath) => $"{outputPath}.ffconcat";

    private static int ChannelCountFromLayout(string layout)
        => layout.Trim().ToLowerInvariant() switch
        {
            "mono" => 1,
            "stereo" => 2,
            "2.1" => 3,
            "3.0" => 3,
            "4.0" => 4,
            "quad" => 4,
            "5.0" => 5,
            "5.1" or "5.1(side)" => 6,
            "7.1" or "7.1(wide)" => 8,
            _ => throw new InvalidOperationException($"Unsupported channel layout: {layout}")
        };

    private static string EscapeFilterPath(string path)
        => path
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(":", "\\:", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);

    private static string EscapeDrawText(string text)
        => text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(":", "\\:", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal);
}
