namespace VidEditor.Domain.Models;

public sealed record BetaExitCriteria(
    double MinimumSuccessRate = 0.95,
    IReadOnlyList<string>? RegressionSensitiveOperations = null)
{
    public IReadOnlyList<string> EffectiveRegressionSensitiveOperations => RegressionSensitiveOperations ?? ["load", "probe", "preview"];
}

