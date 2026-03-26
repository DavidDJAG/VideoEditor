namespace VideoEditor.UI.ViewModels;

public class DashboardViewModel
{
    public string CurrentProjectName => "Untitled project";

    public string TimelineDurationLabel => "00:00:00";

    public IReadOnlyList<string> ReadyTasks =>
    [
        "Import media",
        "Trim clips",
        "Add transitions"
    ];
}
