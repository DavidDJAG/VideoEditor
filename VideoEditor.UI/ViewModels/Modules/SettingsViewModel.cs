using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;
using VideoEditor.Infrastructure.Settings;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ISettingsPersistence _settingsPersistence;
    private readonly IToolchainCapabilitiesService _toolchainCapabilitiesService;
    private readonly DashboardViewModel _dashboardViewModel;
    private string _toolsDirectory;
    private string _scanStatus = "Not scanned";

    public SettingsViewModel(
        ISettingsPersistence settingsPersistence,
        IToolchainCapabilitiesService toolchainCapabilitiesService,
        DashboardViewModel dashboardViewModel)
    {
        _settingsPersistence = settingsPersistence;
        _toolchainCapabilitiesService = toolchainCapabilitiesService;
        _dashboardViewModel = dashboardViewModel;

        var loaded = _settingsPersistence.LoadToolPaths();
        ToolPaths = loaded;
        _toolsDirectory = loaded.ToolsDirectory ?? string.Empty;

        RescanToolsCommand = new AsyncRelayCommand(RescanToolsAsync);
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

    public ICommand RescanToolsCommand { get; }

    public EncodingProfile DefaultProfile { get; set; } =
        new("Balanced", "libx264", "aac", "mp4", "4M", "192k", "yuv420p", "medium");

    private async Task RescanToolsAsync()
    {
        ToolPaths = ToolPaths with { ToolsDirectory = string.IsNullOrWhiteSpace(ToolsDirectory) ? null : ToolsDirectory };
        _settingsPersistence.SaveToolPaths(ToolPaths);

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
