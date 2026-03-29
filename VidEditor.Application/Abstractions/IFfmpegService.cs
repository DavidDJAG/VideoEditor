using VidEditor.Domain.Models;

namespace VidEditor.Application.Abstractions;

public interface IFfmpegService
{
    Task<int> ExecuteAsync(string arguments, CancellationToken cancellationToken = default);

    Task<int> ExecuteOperationAsync(OperationKind kind, OperationParameters operation, CancellationToken cancellationToken = default);
}
