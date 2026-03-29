using VidEditor.Domain.Models;

namespace VidEditor.Application.Abstractions;

public interface IJobStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MediaJob>> GetAllAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(MediaJob job, CancellationToken cancellationToken = default);

    Task<MediaJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
