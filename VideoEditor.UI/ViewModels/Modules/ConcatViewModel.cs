using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

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
