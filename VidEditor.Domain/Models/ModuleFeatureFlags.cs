namespace VidEditor.Domain.Models;

public sealed record ModuleFeatureFlags(
    bool DashboardEnabled = true,
    bool PreviewEnabled = true,
    bool CutTrimEnabled = true,
    bool JoinConcatEnabled = true,
    bool SplitAvEnabled = true,
    bool ConvertEnabled = false,
    bool CutTrimInternalBeta = true,
    bool JoinConcatInternalBeta = true,
    bool SplitAvInternalBeta = true);

