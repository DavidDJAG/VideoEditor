using System.Globalization;
using System.Text.Json;
using VideoEditor.Domain.Models;

namespace VideoEditor.Infrastructure.Services;

public static class FfprobeJsonParser
{
    public static MediaProbeResult Parse(string inputPath, string ffprobeJson, long fallbackSizeBytes = 0)
    {
        using var document = JsonDocument.Parse(ffprobeJson);
        var root = document.RootElement;

        var format = root.TryGetProperty("format", out var formatNode) ? formatNode : default;
        var streams = root.TryGetProperty("streams", out var streamNode) && streamNode.ValueKind == JsonValueKind.Array
            ? streamNode.EnumerateArray().ToArray()
            : Array.Empty<JsonElement>();

        var durationSeconds = ParseDouble(format, "duration") ?? 0;
        var size = ParseLong(format, "size") ?? fallbackSizeBytes;
        var container = ParseString(format, "format_name") ?? Path.GetExtension(inputPath).TrimStart('.');

        var videoStreams = streams.Where(s => ParseString(s, "codec_type") == "video").ToArray();
        var audioStreams = streams.Where(s => ParseString(s, "codec_type") == "audio").ToArray();
        var subtitleStreams = streams.Where(s => ParseString(s, "codec_type") == "subtitle").ToArray();

        var primaryVideo = videoStreams.FirstOrDefault();
        var primaryAudio = audioStreams.FirstOrDefault();

        var frameRate = ParseRatio(primaryVideo, "avg_frame_rate") ?? ParseRatio(primaryVideo, "r_frame_rate");
        var streamInfos = BuildStreamInfos(streams);
        var formatTags = ParseTags(format);

        return new MediaProbeResult(
            inputPath,
            TimeSpan.FromSeconds(durationSeconds),
            size,
            container,
            ParseString(primaryVideo, "codec_name"),
            ParseString(primaryAudio, "codec_name"),
            videoStreams.Length,
            audioStreams.Length,
            subtitleStreams.Length,
            ParseInt(primaryVideo, "width"),
            ParseInt(primaryVideo, "height"),
            frameRate,
            ParseInt(primaryAudio, "sample_rate"),
            ParseInt(primaryAudio, "channels"),
            ParseString(primaryAudio, "channel_layout"),
            ParseString(primaryAudio, "sample_fmt"),
            ffprobeJson,
            streamInfos,
            formatTags);
    }

    private static IReadOnlyList<MediaStreamInfo> BuildStreamInfos(IEnumerable<JsonElement> streams)
    {
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var infos = new List<MediaStreamInfo>();

        foreach (var stream in streams)
        {
            var streamType = ParseString(stream, "codec_type") ?? "unknown";
            if (!counters.TryGetValue(streamType, out var typeIndex))
            {
                typeIndex = 0;
            }

            counters[streamType] = typeIndex + 1;
            var tags = ParseTags(stream);
            var disposition = stream.TryGetProperty("disposition", out var dispositionNode) ? dispositionNode : default;
            var frameRate = ParseRatio(stream, "avg_frame_rate") ?? ParseRatio(stream, "r_frame_rate");

            infos.Add(new MediaStreamInfo(
                SourceIndex: ParseInt(stream, "index") ?? infos.Count,
                StreamType: streamType,
                TypeIndex: typeIndex,
                CodecName: ParseString(stream, "codec_name"),
                CodecLongName: ParseString(stream, "codec_long_name"),
                Language: tags.TryGetValue("language", out var language) ? language : null,
                Title: tags.TryGetValue("title", out var title) ? title : null,
                Width: ParseInt(stream, "width"),
                Height: ParseInt(stream, "height"),
                FrameRate: frameRate,
                SampleRate: ParseInt(stream, "sample_rate"),
                Channels: ParseInt(stream, "channels"),
                ChannelLayout: ParseString(stream, "channel_layout"),
                IsDefault: ParseInt(disposition, "default") == 1,
                IsForced: ParseInt(disposition, "forced") == 1));
        }

        return infos;
    }

    private static Dictionary<string, string> ParseTags(JsonElement element)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (element.ValueKind == JsonValueKind.Undefined || !element.TryGetProperty("tags", out var tagsNode) || tagsNode.ValueKind != JsonValueKind.Object)
        {
            return tags;
        }

        foreach (var property in tagsNode.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                tags[property.Name] = property.Value.GetString() ?? string.Empty;
            }
        }

        return tags;
    }

    private static string? ParseString(JsonElement element, string propertyName)
        => element.ValueKind != JsonValueKind.Undefined && element.TryGetProperty(propertyName, out var node)
            ? node.GetString()
            : null;

    private static int? ParseInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined || !element.TryGetProperty(propertyName, out var node))
        {
            return null;
        }

        return node.ValueKind switch
        {
            JsonValueKind.Number when node.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(node.GetString(), CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static long? ParseLong(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined || !element.TryGetProperty(propertyName, out var node))
        {
            return null;
        }

        return node.ValueKind switch
        {
            JsonValueKind.Number when node.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(node.GetString(), CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static double? ParseDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Undefined || !element.TryGetProperty(propertyName, out var node))
        {
            return null;
        }

        return node.ValueKind switch
        {
            JsonValueKind.Number when node.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(node.GetString(), CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static double? ParseRatio(JsonElement element, string propertyName)
    {
        var value = ParseString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var split = value.Split('/');
        if (split.Length != 2 ||
            !double.TryParse(split[0], CultureInfo.InvariantCulture, out var numerator) ||
            !double.TryParse(split[1], CultureInfo.InvariantCulture, out var denominator) ||
            denominator == 0)
        {
            return null;
        }

        return numerator / denominator;
    }
}
