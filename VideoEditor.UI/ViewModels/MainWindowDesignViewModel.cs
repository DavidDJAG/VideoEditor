using System.Windows.Input;

namespace VideoEditor.UI.ViewModels;

public sealed class MainWindowDesignViewModel
{
    public string Title => "Video Editor";

    public string Subtitle => "Design-time preview with sample project data.";

    public DashboardDesignViewModel DashboardViewModel { get; } = new();

    public DesignSettingsViewModel SettingsViewModel { get; } = new();

    public sealed class DesignSettingsViewModel
    {
        public string ToolsDirectory { get; set; } = @"C:\\ffmpeg\\bin";

        public string ScanStatus { get; set; } = "Last scan: 2026-03-26 12:00:00 UTC";

        public ICommand RescanToolsCommand { get; } = new DesignCommand();

        private sealed class DesignCommand : ICommand
        {
            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) { }
        }
    }
}
