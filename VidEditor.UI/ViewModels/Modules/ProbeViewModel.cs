using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;

namespace VidEditor.UI.ViewModels.Modules;

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
