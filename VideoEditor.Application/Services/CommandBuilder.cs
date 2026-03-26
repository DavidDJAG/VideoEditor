using System.Text;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Services;

public sealed class CommandBuilder : ICommandBuilder
{
    public string BuildTrim(OperationParameters parameters)
    {
        var output = parameters.OutputPath ?? throw new InvalidOperationException("Trim requires OutputPath.");
        var start = parameters.Start ?? TimeSpan.Zero;
        var end = parameters.End ?? throw new InvalidOperationException("Trim requires End time.");
        var duration = end - start;

        return $"-y -ss {start:c} -i \"{parameters.InputPath}\" -t {duration:c} -c copy \"{output}\"";
    }

    public string BuildTranscode(OperationParameters parameters)
    {
        var output = parameters.OutputPath ?? throw new InvalidOperationException("Transcode requires OutputPath.");
        var profile = parameters.EncodingProfile ?? throw new InvalidOperationException("Transcode requires EncodingProfile.");

        return $"-y -i \"{parameters.InputPath}\" -c:v {profile.VideoCodec} -b:v {profile.VideoBitrate} -preset {profile.Preset} -pix_fmt {profile.PixelFormat} -c:a {profile.AudioCodec} -b:a {profile.AudioBitrate} \"{output}\"";
    }

    public string BuildConcat(OperationParameters parameters)
    {
        var output = parameters.OutputPath ?? throw new InvalidOperationException("Concat requires OutputPath.");
        var inputs = parameters.ConcatInputs ?? throw new InvalidOperationException("Concat requires ConcatInputs.");

        var list = string.Join('|', inputs);
        return $"-y -i \"concat:{list}\" -c copy \"{output}\"";
    }

    public string BuildProbe(string inputPath)
    {
        return $"-v quiet -print_format json -show_format -show_streams \"{inputPath}\"";
    }
}
