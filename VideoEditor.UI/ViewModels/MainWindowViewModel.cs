using VideoEditor.UI.ViewModels.Modules;

namespace VideoEditor.UI.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(
        DashboardViewModel dashboardViewModel,
        TrimViewModel trimViewModel,
        TranscodeViewModel transcodeViewModel,
        ConcatViewModel concatViewModel,
        ProbeViewModel probeViewModel,
        QueueViewModel queueViewModel,
        SettingsViewModel settingsViewModel)
    {
        DashboardViewModel = dashboardViewModel;
        TrimViewModel = trimViewModel;
        TranscodeViewModel = transcodeViewModel;
        ConcatViewModel = concatViewModel;
        ProbeViewModel = probeViewModel;
        QueueViewModel = queueViewModel;
        SettingsViewModel = settingsViewModel;
    }

    public string Title => "Video Editor";

    public string Subtitle => "Create timeline edits with a clean MVVM structure.";

    public DashboardViewModel DashboardViewModel { get; }

    public TrimViewModel TrimViewModel { get; }

    public TranscodeViewModel TranscodeViewModel { get; }

    public ConcatViewModel ConcatViewModel { get; }

    public ProbeViewModel ProbeViewModel { get; }

    public QueueViewModel QueueViewModel { get; }

    public SettingsViewModel SettingsViewModel { get; }
}
