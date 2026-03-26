using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class TrimViewModel
{
    private readonly IFfmpegService _ffmpegService;

    public TrimViewModel(IFfmpegService ffmpegService)
    {
        _ffmpegService = ffmpegService;
    }

    public Task<int> ExecuteAsync(OperationParameters parameters, CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteOperationAsync(OperationKind.Trim, parameters, cancellationToken);
}
