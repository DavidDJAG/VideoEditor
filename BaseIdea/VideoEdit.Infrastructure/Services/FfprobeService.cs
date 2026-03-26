using System.Text.Json;
using System.Text.Json.Nodes;
using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Infrastructure.Services;

public sealed class FfprobeService : IFfprobeService
{
    private readonly IFfmpegProcessRunner _processRunner;
    private readonly IToolResolver _toolResolver;

    public FfprobeService(IFfmpegProcessRunner processRunner, IToolResolver toolResolver)
    {
        _processRunner = processRunner;
        _toolResolver = toolResolver;
    }

    public string BuildCommandPreview(string inputPath)
    {
        var toolPaths = _toolResolver.Resolve();
        return $"{toolPaths.FfprobePath} -v error -show_entries format=duration,bit_rate,format_name -show_streams -of json \"{inputPath}\"";
    }

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        var toolPaths = _toolResolver.Resolve();
        var command = new CommandDefinition
        {
            FileName = toolPaths.FfprobePath,
            Arguments = $"-v error -show_entries format=duration,bit_rate,format_name -show_streams -of json \"{inputPath}\"",
            CommandText = BuildCommandPreview(inputPath)
        };

        var result = await _processRunner.RunAsync(command, null, null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StandardError)
                ? "ffprobe devolvió un error."
                : result.StandardError);
        }

        return Parse(inputPath, result.StandardOutput);
    }

    private static MediaProbeResult Parse(string inputPath, string rawJson)
    {
        var root = JsonNode.Parse(rawJson)?.AsObject() ?? [];
        var format = root["format"]?.AsObject();
        var streams = root["streams"]?.AsArray();
        var parsedStreams = new List<MediaStreamInfo>();

        if (streams is not null)
        {
            foreach (var streamNode in streams.OfType<JsonObject>())
            {
                parsedStreams.Add(new MediaStreamInfo
                {
                    Index = streamNode["index"]?.GetValue<int>() ?? 0,
                    CodecType = streamNode["codec_type"]?.GetValue<string>() ?? string.Empty,
                    CodecName = streamNode["codec_name"]?.GetValue<string>() ?? string.Empty,
                    CodecLongName = streamNode["codec_long_name"]?.GetValue<string>(),
                    Width = streamNode["width"]?.GetValue<int>(),
                    Height = streamNode["height"]?.GetValue<int>(),
                    FrameRate = streamNode["avg_frame_rate"]?.GetValue<string>(),
                    BitRate = streamNode["bit_rate"]?.GetValue<string>(),
                    PixelFormat = streamNode["pix_fmt"]?.GetValue<string>(),
                    SampleRate = streamNode["sample_rate"]?.GetValue<string>(),
                    ChannelLayout = streamNode["channel_layout"]?.GetValue<string>()
                });
            }
        }

        var summaryLines = new List<string>
        {
            $"Archivo: {inputPath}",
            $"Contenedor: {format?["format_name"]?.GetValue<string>() ?? "n/d"}",
            $"Duración: {format?["duration"]?.GetValue<string>() ?? "n/d"}",
            $"Bitrate: {format?["bit_rate"]?.GetValue<string>() ?? "n/d"}"
        };

        summaryLines.AddRange(parsedStreams.Select(stream =>
            $"Stream #{stream.Index}: {stream.CodecType} {stream.CodecName} {stream.Width}x{stream.Height} {stream.FrameRate} {stream.PixelFormat}".Trim()));

        return new MediaProbeResult
        {
            InputPath = inputPath,
            ContainerFormat = format?["format_name"]?.GetValue<string>(),
            Duration = format?["duration"]?.GetValue<string>(),
            BitRate = format?["bit_rate"]?.GetValue<string>(),
            Streams = parsedStreams,
            RawJson = JsonSerializer.Serialize(JsonNode.Parse(rawJson), new JsonSerializerOptions { WriteIndented = true }),
            SummaryText = string.Join(Environment.NewLine, summaryLines)
        };
    }
}
