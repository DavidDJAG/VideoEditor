using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IFfprobeService
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default);
}
