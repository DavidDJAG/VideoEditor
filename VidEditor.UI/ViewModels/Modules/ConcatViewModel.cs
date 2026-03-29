using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.UI.ViewModels.Modules;

public sealed class ConcatViewModel
{
    private readonly IFfmpegService _ffmpegService;

    public ConcatViewModel(IFfmpegService ffmpegService)
    {
        _ffmpegService = ffmpegService;
    }

    public Task<int> ExecuteAsync(OperationParameters parameters, CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteOperationAsync(OperationKind.Concat, parameters, cancellationToken);
}
