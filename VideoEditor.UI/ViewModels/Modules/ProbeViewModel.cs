using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class ProbeViewModel
{
    private readonly IFfprobeService _ffprobeService;

    public ProbeViewModel(IFfprobeService ffprobeService)
    {
        _ffprobeService = ffprobeService;
    }

    public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        => _ffprobeService.ProbeAsync(inputPath, cancellationToken);
}
