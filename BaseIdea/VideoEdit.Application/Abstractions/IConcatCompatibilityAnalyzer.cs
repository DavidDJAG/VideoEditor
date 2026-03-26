using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface IConcatCompatibilityAnalyzer
{
    Task<ConcatCompatibilityReport> AnalyzeAsync(IReadOnlyList<string> inputPaths, CancellationToken cancellationToken);
}
