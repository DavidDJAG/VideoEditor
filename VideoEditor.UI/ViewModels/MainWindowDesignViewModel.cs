namespace VideoEditor.UI.ViewModels;

public sealed class MainWindowDesignViewModel
{
    public string Title => "Video Editor";

    public string Subtitle => "Design-time preview with sample project data.";

    public DashboardDesignViewModel DashboardViewModel { get; } = new();
}
