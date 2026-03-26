using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface IFfprobeService
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken);

    string BuildCommandPreview(string inputPath);
}
