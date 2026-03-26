using VideoEdit.Application.Services;
using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Tests;

internal static class Program
{
    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("Todas las pruebas pasaron.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static void Run()
    {
        var builder = new FfmpegArgumentBuilder();
        var validator = new MediaJobValidator();
        var toolPaths = new ToolPaths();

        var trimRequest = new MediaJobRequest
        {
            JobType = MediaJobType.TrimVideo,
            InputPaths = [@"C:\input.mp4"],
            OutputPath = @"C:\output.mp4",
            TimeRange = new TimeRange
            {
                Start = TimeSpan.FromSeconds(10),
                End = TimeSpan.FromMinutes(1)
            },
            VideoCopy = true,
            AudioCopy = true
        };

        var trimCommand = builder.Build(trimRequest, toolPaths).CommandText;
        AssertEx.Contains("-ss 00:00:10", trimCommand, "El recorte debe incluir tiempo inicial.");
        AssertEx.Contains("-to 00:01:00", trimCommand, "El recorte debe incluir tiempo final.");
        AssertEx.Contains("-c copy", trimCommand, "El recorte rápido debe usar copy.");

        var extractAudioRequest = new MediaJobRequest
        {
            JobType = MediaJobType.ExtractAudio,
            InputPaths = [@"C:\input.mp4"],
            OutputPath = @"C:\audio.mp3",
            AudioCodec = "libmp3lame",
            AudioBitrate = "q=2"
        };
        var audioCommand = builder.Build(extractAudioRequest, toolPaths).CommandText;
        AssertEx.Contains("-q:a 2", audioCommand, "MP3 de alta calidad debe usar q:a 2.");

        var invalidRequest = new MediaJobRequest
        {
            JobType = MediaJobType.Convert,
            InputPaths = [@"C:\missing.mp4"],
            OutputPath = @"C:\result.mp4",
            ContainerExtension = "mp4",
            AudioCodec = "libopus",
            VideoCodec = "libx264"
        };
        var validation = validator.Validate(invalidRequest);
        AssertEx.True(!validation.IsValid, "La validación debe fallar con entrada faltante y contenedor inválido.");
        AssertEx.True(validation.Errors.Count >= 2, "Se esperaban múltiples errores de validación.");

        var presets = EncodingPreset.CreateDefaults();
        AssertEx.True(presets.Any(p => p.Name.Contains("AV1", StringComparison.OrdinalIgnoreCase)), "Debe existir preset AV1.");
        AssertEx.True(presets.Any(p => p.JobType == MediaJobType.ExtractAudio), "Debe haber presets de extracción de audio.");

        var analyzer = new ConcatCompatibilityAnalyzer(new FakeFfprobeService(
        [
            new MediaProbeResult
            {
                InputPath = @"C:\a.mp4",
                ContainerFormat = "mov,mp4,m4a,3gp,3g2,mj2",
                Streams =
                [
                    new MediaStreamInfo { CodecType = "video", CodecName = "h264", Width = 1920, Height = 1080, FrameRate = "30000/1001", PixelFormat = "yuv420p" },
                    new MediaStreamInfo { CodecType = "audio", CodecName = "aac", SampleRate = "48000", ChannelLayout = "stereo" }
                ]
            },
            new MediaProbeResult
            {
                InputPath = @"C:\b.mp4",
                ContainerFormat = "mov,mp4,m4a,3gp,3g2,mj2",
                Streams =
                [
                    new MediaStreamInfo { CodecType = "video", CodecName = "hevc", Width = 1920, Height = 1080, FrameRate = "25/1", PixelFormat = "yuv420p" },
                    new MediaStreamInfo { CodecType = "audio", CodecName = "aac", SampleRate = "44100", ChannelLayout = "stereo" }
                ]
            }
        ]));
        var concatReport = analyzer.AnalyzeAsync([@"C:\a.mp4", @"C:\b.mp4"], CancellationToken.None).GetAwaiter().GetResult();
        AssertEx.True(!concatReport.IsCompatible, "El analizador debe detectar incompatibilidades en concat.");
        AssertEx.True(concatReport.Issues.Any(issue => issue.Category.Contains("Codec de video", StringComparison.OrdinalIgnoreCase)), "Debe reportar codec de video incompatible.");
    }

    private sealed class FakeFfprobeService : IFfprobeService
    {
        private readonly Dictionary<string, MediaProbeResult> _results;

        public FakeFfprobeService(IEnumerable<MediaProbeResult> results)
        {
            _results = results.ToDictionary(result => result.InputPath, StringComparer.OrdinalIgnoreCase);
        }

        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken)
        {
            return Task.FromResult(_results[inputPath]);
        }

        public string BuildCommandPreview(string inputPath) => inputPath;
    }
}
