using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IConcatCompatibilityService
{
    Task<ConcatCompatibilityResult> CheckStreamCopyCompatibilityAsync(IReadOnlyList<string> orderedInputs, CancellationToken cancellationToken = default);

    Task EnsureStreamCopyCompatibilityAsync(IReadOnlyList<string> orderedInputs, CancellationToken cancellationToken = default);
}
