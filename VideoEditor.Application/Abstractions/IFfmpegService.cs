using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IFfmpegService
{
    Task<int> ExecuteAsync(string arguments, CancellationToken cancellationToken = default);

    Task<int> ExecuteOperationAsync(OperationKind kind, OperationParameters operation, CancellationToken cancellationToken = default);
}
