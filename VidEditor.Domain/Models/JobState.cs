namespace VidEditor.Domain.Models;

public enum JobState
{
    Draft,
    Queued,
    Running,
    Paused,
    Succeeded,
    Failed,
    Cancelled
}
