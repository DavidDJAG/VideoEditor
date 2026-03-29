namespace VidEditor.Domain.Models;

public sealed record ConcatCompatibilityResult(bool IsCompatible, IReadOnlyList<string> IncompatibilityReasons)
{
    public static ConcatCompatibilityResult Compatible { get; } = new(true, []);
}

public sealed class ConcatCompatibilityException : InvalidOperationException
{
    public ConcatCompatibilityException(IReadOnlyList<string> reasons)
        : base(BuildMessage(reasons))
    {
        Reasons = reasons;
    }

    public IReadOnlyList<string> Reasons { get; }

    private static string BuildMessage(IReadOnlyList<string> reasons)
    {
        var detail = reasons.Count == 0
            ? "Unknown compatibility issue."
            : string.Join("; ", reasons);

        return $"Concat with -c copy is blocked due to incompatibility: {detail}. Explicit alternative: transcode all inputs to a common profile/container and then run concat.";
    }
}
