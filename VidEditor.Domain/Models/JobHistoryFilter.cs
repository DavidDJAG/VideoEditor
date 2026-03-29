namespace VidEditor.Domain.Models;

public sealed record JobHistoryFilter(
    string? SearchText = null,
    IReadOnlyCollection<JobState>? States = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null);
