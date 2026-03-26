using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class SplitAvViewModel
{
    private readonly IFfmpegService _ffmpegService;
    private readonly ICommandBuilder _commandBuilder;

    public SplitAvViewModel(IFfmpegService ffmpegService, ICommandBuilder commandBuilder)
    {
        _ffmpegService = ffmpegService;
        _commandBuilder = commandBuilder;
    }

    public Task<int> ExtractAudioAsync(string inputPath, string outputPath, string audioCodec = "copy", CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteAsync(_commandBuilder.Build(new ExtractAudioRequest(inputPath, outputPath, audioCodec)), cancellationToken);

    public Task<int> ExtractVideoAsync(string inputPath, string outputPath, string videoCodec = "copy", CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteAsync(_commandBuilder.Build(new ExtractVideoRequest(inputPath, outputPath, videoCodec)), cancellationToken);
}
