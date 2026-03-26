using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IToolchainCapabilitiesService
{
    Task<ToolchainCapabilitiesSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<ToolchainCapabilitiesSnapshot> RefreshAsync(CancellationToken cancellationToken = default);
}
