using VidEditor.Domain.Models;

namespace VidEditor.Application.Abstractions;

public interface IToolchainCapabilitiesService
{
    Task<ToolchainCapabilitiesSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<ToolchainCapabilitiesSnapshot> RefreshAsync(CancellationToken cancellationToken = default);
}
