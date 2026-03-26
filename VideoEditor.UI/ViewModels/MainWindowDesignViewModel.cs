using System.Windows.Input;

namespace VideoEditor.UI.ViewModels;

public sealed class MainWindowDesignViewModel
{
    public string Title => "Video Editor";

    public DashboardDesignViewModel DashboardViewModel { get; } = new();

    public DesignSettingsViewModel SettingsViewModel { get; } = new();

    public DesignModulesWorkbenchViewModel ModulesWorkbenchViewModel { get; } = new();

    public DesignPreviewViewModel PreviewViewModel { get; } = new();

    public sealed class DesignSettingsViewModel
    {
        public string ToolsDirectory { get; set; } = @"C:\\ffmpeg\\bin";

        public string ScanStatus { get; set; } = "Last scan: 2026-03-26 12:00:00 UTC";

        public string BetaStatus { get; set; } = "Beta not evaluated";

        public bool IsDashboardEnabled { get; set; } = true;

        public bool IsPreviewEnabled { get; set; } = true;

        public bool IsCutTrimEnabled { get; set; } = true;

        public bool IsCutTrimInternalBeta { get; set; } = true;

        public bool IsJoinConcatEnabled { get; set; } = true;

        public bool IsJoinConcatInternalBeta { get; set; } = true;

        public bool IsSplitAvEnabled { get; set; } = true;

        public bool IsSplitAvInternalBeta { get; set; } = true;

        public bool IsConvertEnabled { get; set; } = false;

        public double MinimumSuccessRateForBetaExit { get; set; } = 0.95;

        public ICommand RescanToolsCommand { get; } = new DesignCommand();

        public ICommand EvaluateBetaCommand { get; } = new DesignCommand();
    }

    public sealed class DesignModulesWorkbenchViewModel
    {
        public string TrimInputPath { get; set; } = @"C:\\media\\input.mp4";
        public string TrimOutputPath { get; set; } = @"C:\\media\\trimmed.mp4";
        public TimeSpan TrimStart { get; set; } = TimeSpan.Zero;
        public TimeSpan TrimEnd { get; set; } = TimeSpan.FromSeconds(10);
        public string TrimStatus { get; set; } = "Idle";

        public string ConcatInputA { get; set; } = @"C:\\media\\part-a.mp4";
        public string ConcatInputB { get; set; } = @"C:\\media\\part-b.mp4";
        public string ConcatOutputPath { get; set; } = @"C:\\media\\joined.mp4";
        public string ConcatStatus { get; set; } = "Idle";

        public string ConvertInputPath { get; set; } = @"C:\\media\\source.mkv";
        public string ConvertOutputPath { get; set; } = @"C:\\media\\converted.mp4";
        public string ConvertStatus { get; set; } = "Idle";

        public ICommand RunTrimCommand { get; } = new DesignCommand();
        public ICommand OpenTrimInputCommand { get; } = new DesignCommand();
        public ICommand SaveTrimOutputCommand { get; } = new DesignCommand();
        public ICommand RunConcatCommand { get; } = new DesignCommand();
        public ICommand OpenConcatInputACommand { get; } = new DesignCommand();
        public ICommand OpenConcatInputBCommand { get; } = new DesignCommand();
        public ICommand SaveConcatOutputCommand { get; } = new DesignCommand();
        public ICommand RunConvertCommand { get; } = new DesignCommand();
        public ICommand OpenConvertInputCommand { get; } = new DesignCommand();
        public ICommand SaveConvertOutputCommand { get; } = new DesignCommand();
    }

    public sealed class DesignPreviewViewModel
    {
        public string InputPath { get; set; } = @"C:\\media\\input.mp4";
        public TimeSpan? InMarker { get; set; } = TimeSpan.Zero;
        public TimeSpan? OutMarker { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan SubtitleOffset { get; set; } = TimeSpan.Zero;
        public double SpeedFactor { get; set; } = 1.0;
        public TimeSpan? AStart { get; set; } = TimeSpan.Zero;
        public TimeSpan? AEnd { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan? BStart { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan? BEnd { get; set; } = TimeSpan.FromSeconds(15);
        public TimeSpan? SelectedSeek { get; set; } = TimeSpan.FromSeconds(12);
        public IEnumerable<TimeSpan> ProbeSeekPoints { get; } = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(15)
        };

        public string Status { get; set; } = "Idle";

        public ICommand BrowseInputCommand { get; } = new DesignCommand();
        public ICommand LoadProbeCommand { get; } = new DesignCommand();
        public ICommand PlaySelectionCommand { get; } = new DesignCommand();
        public ICommand PlayAbCommand { get; } = new DesignCommand();
        public ICommand QuickSeekCommand { get; } = new DesignCommand();
        public ICommand StopCommand { get; } = new DesignCommand();
    }

    private sealed class DesignCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
    }
}
