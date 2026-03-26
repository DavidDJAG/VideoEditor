using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class SplitAvViewModel
{
    private readonly IFfmpegService _ffmpegService;

    public SplitAvViewModel(IFfmpegService ffmpegService)
    {
        _ffmpegService = ffmpegService;
    }

    public Task<int> ExtractAudioAsync(string inputPath, string outputPath, string audioCodec = "copy", CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteOperationAsync(
            OperationKind.ExtractAudio,
            new OperationParameters(
                inputPath,
                outputPath,
                Start: null,
                End: null,
                SubtitleOffset: TimeSpan.Zero,
                SpeedFactor: 1.0,
                AdditionalInputs: [],
                Flags: new Dictionary<string, string> { ["audioCodec"] = audioCodec },
                EncodingProfile: null),
            cancellationToken);

    public Task<int> ExtractVideoAsync(string inputPath, string outputPath, string videoCodec = "copy", CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteOperationAsync(
            OperationKind.ExtractVideo,
            new OperationParameters(
                inputPath,
                outputPath,
                Start: null,
                End: null,
                SubtitleOffset: TimeSpan.Zero,
                SpeedFactor: 1.0,
                AdditionalInputs: [],
                Flags: new Dictionary<string, string> { ["videoCodec"] = videoCodec },
                EncodingProfile: null),
            cancellationToken);
}
