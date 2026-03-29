using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.UI.ViewModels.Modules;

public sealed class TranscodeViewModel
{
    private readonly IFfmpegService _ffmpegService;

    public TranscodeViewModel(IFfmpegService ffmpegService)
    {
        _ffmpegService = ffmpegService;
    }

    public Task<int> ExecuteAsync(OperationParameters parameters, CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteOperationAsync(OperationKind.Convert, parameters, cancellationToken);
}
