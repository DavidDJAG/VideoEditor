using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface ILogStore
{
    Task AppendAsync(LogEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<LogEntry>> LoadRecentAsync(int maxEntries, CancellationToken cancellationToken);
}
