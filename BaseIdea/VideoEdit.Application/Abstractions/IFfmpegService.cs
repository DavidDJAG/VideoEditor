using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface IFfmpegService
{
    event EventHandler<FfmpegProgressInfo>? ProgressChanged;

    Task<MediaJobResult> ExecuteJobAsync(MediaJob job, CancellationToken cancellationToken);

    string BuildCommandPreview(MediaJobRequest request);
}
