namespace VideoEditor.UI.ViewModels;

public sealed class MainWindowViewModel
{
    public string Title => "Video Editor";

    public string Subtitle => "Create timeline edits with a clean MVVM structure.";

    public DashboardViewModel DashboardViewModel { get; } = new();
}
