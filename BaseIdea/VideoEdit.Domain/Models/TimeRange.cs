namespace VideoEdit.Domain.Models;

public sealed class TimeRange
{
    public TimeSpan? Start { get; init; }

    public TimeSpan? End { get; init; }

    public TimeSpan? Duration { get; init; }
}
