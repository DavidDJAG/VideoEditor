namespace VideoEditor.Domain.Models;

public sealed record RetryPolicy(int MaxAttempts = 1, TimeSpan? DelayBetweenAttempts = null)
{
    public static RetryPolicy Default { get; } = new(1, TimeSpan.Zero);

    public TimeSpan Delay => DelayBetweenAttempts ?? TimeSpan.Zero;
}
