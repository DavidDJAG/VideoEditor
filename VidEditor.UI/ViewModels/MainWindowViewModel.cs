using VidEditor.UI.ViewModels.Modules;

namespace VidEditor.UI.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(
        DashboardViewModel dashboardViewModel,
        TrimViewModel trimViewModel,
        TranscodeViewModel transcodeViewModel,
        ConcatViewModel concatViewModel,
        ProbeViewModel probeViewModel,
        PreviewViewModel previewViewModel,
        QueueViewModel queueViewModel,
        SettingsViewModel settingsViewModel,
        ModulesWorkbenchViewModel modulesWorkbenchViewModel)
    {
        DashboardViewModel = dashboardViewModel;
        TrimViewModel = trimViewModel;
        TranscodeViewModel = transcodeViewModel;
        ConcatViewModel = concatViewModel;
        ProbeViewModel = probeViewModel;
        PreviewViewModel = previewViewModel;
        QueueViewModel = queueViewModel;
        SettingsViewModel = settingsViewModel;
        ModulesWorkbenchViewModel = modulesWorkbenchViewModel;
    }

    public string Title => "Video Editor";

    public string Subtitle => "Create timeline edits with a clean MVVM structure.";

    public DashboardViewModel DashboardViewModel { get; }

    public TrimViewModel TrimViewModel { get; }

    public TranscodeViewModel TranscodeViewModel { get; }

    public ConcatViewModel ConcatViewModel { get; }

    public ProbeViewModel ProbeViewModel { get; }

    public PreviewViewModel PreviewViewModel { get; }

    public QueueViewModel QueueViewModel { get; }

    public SettingsViewModel SettingsViewModel { get; }

    public ModulesWorkbenchViewModel ModulesWorkbenchViewModel { get; }
}
