using VidEditor.Domain.Models;

namespace VidEditor.Application.Abstractions;

public interface IFfprobeService
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default);
}
