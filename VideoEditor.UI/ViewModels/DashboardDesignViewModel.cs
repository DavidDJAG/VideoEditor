namespace VideoEditor.UI.ViewModels;

public sealed class DashboardDesignViewModel : DashboardViewModel
{
    public new string CurrentProjectName => "Product Launch - Episode 02";

    public new string TimelineDurationLabel => "00:08:42";

    public new IReadOnlyList<string> ReadyTasks =>
    [
        "Color correction",
        "Title animation",
        "Audio balance"
    ];
}
