using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class BetaRolloutDefaultsTests
{
    [Fact]
    public void AppSettings_Default_EnablesStableAndInternalBetaModules()
    {
        var defaults = AppSettings.Default;

        Assert.True(defaults.ModuleFlags.DashboardEnabled);
        Assert.True(defaults.ModuleFlags.PreviewEnabled);
        Assert.True(defaults.ModuleFlags.CutTrimEnabled);
        Assert.True(defaults.ModuleFlags.JoinConcatEnabled);
        Assert.True(defaults.ModuleFlags.SplitAvEnabled);
        Assert.True(defaults.ModuleFlags.CutTrimInternalBeta);
        Assert.True(defaults.ModuleFlags.JoinConcatInternalBeta);
        Assert.True(defaults.ModuleFlags.SplitAvInternalBeta);
        Assert.False(defaults.ModuleFlags.ConvertEnabled);
    }

    [Fact]
    public void BetaExitCriteria_Default_TracksProbePreviewAndLoadRegressions()
    {
        var criteria = new BetaExitCriteria();

        Assert.Equal(0.95, criteria.MinimumSuccessRate, 3);
        Assert.Contains("load", criteria.EffectiveRegressionSensitiveOperations);
        Assert.Contains("probe", criteria.EffectiveRegressionSensitiveOperations);
        Assert.Contains("preview", criteria.EffectiveRegressionSensitiveOperations);
    }
}
