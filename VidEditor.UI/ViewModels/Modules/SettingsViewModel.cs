using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VidEditor.Application.Abstractions;
using VidEditor.Domain.Models;
using VidEditor.Infrastructure.Settings;

namespace VidEditor.UI.ViewModels.Modules;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ISettingsPersistence _settingsPersistence;
    private readonly IToolchainCapabilitiesService _toolchainCapabilitiesService;
    private readonly IJobQueueService _jobQueueService;
    private readonly DashboardViewModel _dashboardViewModel;
    private ModuleFeatureFlags _moduleFlags;
    private BetaExitCriteria _betaCriteria;
    private string _toolsDirectory;
    private string _scanStatus = "Not scanned";
    private string _betaStatus = "Beta not evaluated";

    public SettingsViewModel(
        ISettingsPersistence settingsPersistence,
        IToolchainCapabilitiesService toolchainCapabilitiesService,
        IJobQueueService jobQueueService,
        DashboardViewModel dashboardViewModel)
    {
        _settingsPersistence = settingsPersistence;
        _toolchainCapabilitiesService = toolchainCapabilitiesService;
        _jobQueueService = jobQueueService;
        _dashboardViewModel = dashboardViewModel;

        var settings = _settingsPersistence.LoadAppSettings();
        ToolPaths = settings.ToolPaths;
        _moduleFlags = settings.ModuleFlags;
        _betaCriteria = settings.BetaCriteria;
        _toolsDirectory = settings.ToolPaths.ToolsDirectory ?? string.Empty;

        RescanToolsCommand = new AsyncRelayCommand(RescanToolsAsync);
        EvaluateBetaCommand = new AsyncRelayCommand(EvaluateBetaReadinessAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ToolPaths ToolPaths { get; private set; }

    public string ToolsDirectory
    {
        get => _toolsDirectory;
        set => Set(ref _toolsDirectory, value);
    }

    public string ScanStatus
    {
        get => _scanStatus;
        private set => Set(ref _scanStatus, value);
    }

    public string BetaStatus
    {
        get => _betaStatus;
        private set => Set(ref _betaStatus, value);
    }

    public ICommand RescanToolsCommand { get; }

    public ICommand EvaluateBetaCommand { get; }

    public bool IsDashboardEnabled
    {
        get => _moduleFlags.DashboardEnabled;
        set => UpdateModuleFlags(_moduleFlags with { DashboardEnabled = value }, nameof(IsDashboardEnabled));
    }

    public bool IsPreviewEnabled
    {
        get => _moduleFlags.PreviewEnabled;
        set => UpdateModuleFlags(_moduleFlags with { PreviewEnabled = value }, nameof(IsPreviewEnabled));
    }

    public bool IsCutTrimEnabled
    {
        get => _moduleFlags.CutTrimEnabled;
        set => UpdateModuleFlags(_moduleFlags with { CutTrimEnabled = value }, nameof(IsCutTrimEnabled));
    }

    public bool IsJoinConcatEnabled
    {
        get => _moduleFlags.JoinConcatEnabled;
        set => UpdateModuleFlags(_moduleFlags with { JoinConcatEnabled = value }, nameof(IsJoinConcatEnabled));
    }

    public bool IsSplitAvEnabled
    {
        get => _moduleFlags.SplitAvEnabled;
        set => UpdateModuleFlags(_moduleFlags with { SplitAvEnabled = value }, nameof(IsSplitAvEnabled));
    }

    public bool IsConvertEnabled
    {
        get => _moduleFlags.ConvertEnabled;
        set => UpdateModuleFlags(_moduleFlags with { ConvertEnabled = value }, nameof(IsConvertEnabled));
    }

    public bool IsCutTrimInternalBeta
    {
        get => _moduleFlags.CutTrimInternalBeta;
        set => UpdateModuleFlags(_moduleFlags with { CutTrimInternalBeta = value }, nameof(IsCutTrimInternalBeta));
    }

    public bool IsJoinConcatInternalBeta
    {
        get => _moduleFlags.JoinConcatInternalBeta;
        set => UpdateModuleFlags(_moduleFlags with { JoinConcatInternalBeta = value }, nameof(IsJoinConcatInternalBeta));
    }

    public bool IsSplitAvInternalBeta
    {
        get => _moduleFlags.SplitAvInternalBeta;
        set => UpdateModuleFlags(_moduleFlags with { SplitAvInternalBeta = value }, nameof(IsSplitAvInternalBeta));
    }

    public double MinimumSuccessRateForBetaExit
    {
        get => _betaCriteria.MinimumSuccessRate;
        set
        {
            var normalized = Math.Clamp(value, 0, 1);
            if (Math.Abs(normalized - _betaCriteria.MinimumSuccessRate) < 0.0001)
            {
                return;
            }

            _betaCriteria = _betaCriteria with { MinimumSuccessRate = normalized };
            SaveSettings();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinimumSuccessRateForBetaExit)));
        }
    }

    public ConvertOptions DefaultProfile { get; set; } =
        ConvertOptions.CreateBalancedMp4H264();

    private async Task RescanToolsAsync()
    {
        SaveSettings();

        try
        {
            var snapshot = await _toolchainCapabilitiesService.RefreshAsync();
            _dashboardViewModel.ApplySnapshot(snapshot);
            ScanStatus = $"Last scan: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss} UTC";
        }
        catch (Exception ex)
        {
            _dashboardViewModel.ApplyError(ex.Message);
            ScanStatus = $"Scan failed: {ex.Message}";
        }
    }

    private async Task EvaluateBetaReadinessAsync()
    {
        var jobs = await _jobQueueService.GetAllAsync();
        var queuedArtifacts = jobs
            .Where(job => job.LastArtifact is not null)
            .Select(job => new
            {
                job.Operation,
                Artifact = job.LastArtifact!,
                IsSuccess = job.LastArtifact!.ExitCode == 0
            })
            .ToArray();

        if (queuedArtifacts.Length == 0)
        {
            BetaStatus = "Sin artefactos de cola para evaluar beta.";
            return;
        }

        var grouped = queuedArtifacts
            .GroupBy(item => item.Operation, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var total = group.Count();
                var successful = group.Count(entry => entry.IsSuccess);
                var failed = total - successful;
                return new
                {
                    Operation = group.Key,
                    Total = total,
                    Successful = successful,
                    Failed = failed,
                    SuccessRate = total == 0 ? 0 : (double)successful / total,
                    LastFailure = group
                        .Where(entry => !entry.IsSuccess)
                        .Select(entry => string.IsNullOrWhiteSpace(entry.Artifact.StandardError) ? entry.Artifact.StandardOutput : entry.Artifact.StandardError)
                        .LastOrDefault(message => !string.IsNullOrWhiteSpace(message))
                };
            })
            .OrderBy(item => item.Operation)
            .ToArray();

        var allTotal = grouped.Sum(item => item.Total);
        var allSuccess = grouped.Sum(item => item.Successful);
        var globalRate = allTotal == 0 ? 0 : (double)allSuccess / allTotal;

        var regressionSensitive = _betaCriteria.EffectiveRegressionSensitiveOperations;
        var regressions = grouped.Where(item =>
            regressionSensitive.Any(keyword => item.Operation.Contains(keyword, StringComparison.OrdinalIgnoreCase)) &&
            item.Failed > 0).ToArray();

        var betaReady = globalRate >= _betaCriteria.MinimumSuccessRate && regressions.Length == 0;
        var operationSummary = string.Join(" | ", grouped.Select(item =>
            $"{item.Operation}: {item.Successful}/{item.Total} ({item.SuccessRate:P0})"));

        var failures = string.Join(" | ", grouped
            .Where(item => item.Failed > 0)
            .Select(item => $"{item.Operation}: {item.LastFailure}"));

        BetaStatus = betaReady
            ? $"BETA READY ✅ Global success {(globalRate):P1}. Ops: {operationSummary}"
            : $"BETA HOLD ⏸ Global success {(globalRate):P1} (< {_betaCriteria.MinimumSuccessRate:P0} or regressions). Regressions: {string.Join(", ", regressions.Select(x => x.Operation))}. Failures: {failures}. Ops: {operationSummary}";
    }

    private void UpdateModuleFlags(ModuleFeatureFlags updated, string propertyName)
    {
        if (updated == _moduleFlags)
        {
            return;
        }

        _moduleFlags = updated;
        SaveSettings();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SaveSettings()
    {
        ToolPaths = ToolPaths with { ToolsDirectory = string.IsNullOrWhiteSpace(ToolsDirectory) ? null : ToolsDirectory };
        _settingsPersistence.SaveAppSettings(new AppSettings(ToolPaths, _moduleFlags, _betaCriteria, _settingsPersistence.LoadConvertPresets()));
    }

    private void Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
