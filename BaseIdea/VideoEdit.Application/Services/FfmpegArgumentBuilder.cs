using System.Globalization;
using System.Text;
using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Services;

public sealed class FfmpegArgumentBuilder : IFfmpegCommandBuilder
{
    public CommandDefinition Build(MediaJobRequest request, ToolPaths toolPaths)
    {
        var arguments = request.JobType switch
        {
            MediaJobType.TrimVideo => BuildTrim(request),
            MediaJobType.ExtractAudio => BuildExtractAudio(request),
            MediaJobType.ExtractVideo => BuildExtractVideo(request),
            MediaJobType.Convert => BuildConvert(request),
            MediaJobType.Concat => BuildConcat(request),
            _ => throw new InvalidOperationException($"Tipo de job no soportado: {request.JobType}")
        };

        return new CommandDefinition
        {
            FileName = toolPaths.FfmpegPath,
            Arguments = arguments,
            CommandText = $"{toolPaths.FfmpegPath} {arguments}".Trim()
        };
    }

    private static string BuildTrim(MediaJobRequest request)
    {
        var builder = new StringBuilder();
        AppendOverwrite(builder, request);

        if (!request.AccurateSeek && request.TimeRange?.Start is not null)
        {
            builder.Append($"-ss {FormatTime(request.TimeRange.Start.Value)} ");
        }

        builder.Append($"-i {Quote(request.InputPaths[0])} ");

        if (request.AccurateSeek && request.TimeRange?.Start is not null)
        {
            builder.Append($"-ss {FormatTime(request.TimeRange.Start.Value)} ");
        }

        if (request.TimeRange?.End is not null)
        {
            builder.Append($"-to {FormatTime(request.TimeRange.End.Value)} ");
        }
        else if (request.TimeRange?.Duration is not null)
        {
            builder.Append($"-t {FormatTime(request.TimeRange.Duration.Value)} ");
        }

        builder.Append(request.VideoCopy && request.AudioCopy ? "-c copy " : BuildCodecSection(request));
        builder.Append(Quote(request.OutputPath));
        return builder.ToString().Trim();
    }

    private static string BuildExtractAudio(MediaJobRequest request)
    {
        var builder = new StringBuilder();
        AppendOverwrite(builder, request);
        builder.Append($"-i {Quote(request.InputPaths[0])} -vn ");

        if (request.AudioCopy || string.Equals(request.AudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
        {
            builder.Append("-c:a copy ");
        }
        else
        {
            builder.Append($"-c:a {request.AudioCodec ?? "aac"} ");
            if (string.Equals(request.AudioBitrate, "q=2", StringComparison.OrdinalIgnoreCase))
            {
                builder.Append("-q:a 2 ");
            }
            else if (!string.IsNullOrWhiteSpace(request.AudioBitrate))
            {
                builder.Append($"-b:a {request.AudioBitrate} ");
            }
        }

        builder.Append(Quote(request.OutputPath));
        return builder.ToString().Trim();
    }

    private static string BuildExtractVideo(MediaJobRequest request)
    {
        var builder = new StringBuilder();
        AppendOverwrite(builder, request);
        builder.Append($"-i {Quote(request.InputPaths[0])} -an ");
        builder.Append(request.VideoCopy || string.Equals(request.VideoCodec, "copy", StringComparison.OrdinalIgnoreCase)
            ? "-c:v copy "
            : BuildCodecSection(request, includeAudio: false));
        builder.Append(Quote(request.OutputPath));
        return builder.ToString().Trim();
    }

    private static string BuildConvert(MediaJobRequest request)
    {
        var builder = new StringBuilder();
        AppendOverwrite(builder, request);
        builder.Append($"-i {Quote(request.InputPaths[0])} ");
        builder.Append(BuildCodecSection(request));
        builder.Append(Quote(request.OutputPath));
        return builder.ToString().Trim();
    }

    private static string BuildConcat(MediaJobRequest request)
    {
        var concatFile = request.OutputPath + ".concat.txt";
        var builder = new StringBuilder();
        AppendOverwrite(builder, request);
        builder.Append($"-f concat -safe 0 -i {Quote(concatFile)} -c copy {Quote(request.OutputPath)}");
        return builder.ToString().Trim();
    }

    private static string BuildCodecSection(MediaJobRequest request, bool includeAudio = true)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(request.VideoCodec))
        {
            builder.Append($"-c:v {request.VideoCodec} ");
        }

        if (!string.IsNullOrWhiteSpace(request.Crf))
        {
            builder.Append($"-crf {request.Crf} ");
        }

        if (!string.IsNullOrWhiteSpace(request.Preset))
        {
            builder.Append($"-preset {request.Preset} ");
        }

        if (!string.IsNullOrWhiteSpace(request.PixelFormat))
        {
            builder.Append($"-pix_fmt {request.PixelFormat} ");
        }

        if (includeAudio)
        {
            if (string.IsNullOrWhiteSpace(request.AudioCodec) && !string.IsNullOrWhiteSpace(request.VideoCodec))
            {
                builder.Append("-c:a copy ");
            }
            else if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                builder.Append($"-c:a {request.AudioCodec} ");
            }

            if (!string.IsNullOrWhiteSpace(request.AudioBitrate) && !string.Equals(request.AudioBitrate, "q=2", StringComparison.OrdinalIgnoreCase))
            {
                builder.Append($"-b:a {request.AudioBitrate} ");
            }
        }

        return builder.ToString();
    }

    private static void AppendOverwrite(StringBuilder builder, MediaJobRequest request)
    {
        builder.Append(request.OverwriteOutput ? "-y " : "-n ");
    }

    private static string Quote(string value) => $"\"{value}\"";

    private static string FormatTime(TimeSpan value) =>
        value.ToString(value.Milliseconds == 0 ? @"hh\:mm\:ss" : @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
}
