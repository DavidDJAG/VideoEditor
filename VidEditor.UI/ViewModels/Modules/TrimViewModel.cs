using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.UI.ViewModels.Modules;

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
