using System.ComponentModel;
using System.Runtime.CompilerServices;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IToolchainCapabilitiesService _toolchainCapabilitiesService;
    private string _ffmpegPath = "Not scanned";
    private string _ffprobePath = "Not scanned";
    private string _ffmpegVersion = "Unknown";
    private string _codecSupportSummary = "Not scanned";
    private string _hardwareAccelerationSummary = "Not scanned";
    private string _blockingError = string.Empty;

    public DashboardViewModel(IToolchainCapabilitiesService toolchainCapabilitiesService)
    {
        _toolchainCapabilitiesService = toolchainCapabilitiesService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentProjectName => "Untitled project";

    public string TimelineDurationLabel => "00:00:00";

    public IReadOnlyList<string> ReadyTasks =>
    [
        "Import media",
        "Trim clips",
        "Add transitions"
    ];

    public string FfmpegPath
    {
        get => _ffmpegPath;
        private set => Set(ref _ffmpegPath, value);
    }

    public string FfprobePath
    {
        get => _ffprobePath;
        private set => Set(ref _ffprobePath, value);
    }

    public string FfmpegVersion
    {
        get => _ffmpegVersion;
        private set => Set(ref _ffmpegVersion, value);
    }

    public string CodecSupportSummary
    {
        get => _codecSupportSummary;
        private set => Set(ref _codecSupportSummary, value);
    }

    public string HardwareAccelerationSummary
    {
        get => _hardwareAccelerationSummary;
        private set => Set(ref _hardwareAccelerationSummary, value);
    }

    public string BlockingError
    {
        get => _blockingError;
        private set => Set(ref _blockingError, value);
    }

    public void ApplySnapshot(ToolchainCapabilitiesSnapshot snapshot)
    {
        FfmpegPath = snapshot.Ffmpeg.ResolvedPath;
        FfprobePath = snapshot.Ffprobe.ResolvedPath;
        FfmpegVersion = snapshot.FfmpegVersion;

        var reportedVideoCapabilities = snapshot.VideoEncoders.Count == 0
            ? snapshot.SupportedVideoCodecs
            : snapshot.VideoEncoders;

        CodecSupportSummary = reportedVideoCapabilities.Count == 0
            ? "No codecs reported"
            : $"{reportedVideoCapabilities.Count} video capabilities (e.g. {string.Join(", ", reportedVideoCapabilities.Take(6))})";

        HardwareAccelerationSummary = snapshot.HardwareAccelerationMethods.Count == 0
            ? "No hardware acceleration detected"
            : string.Join(", ", snapshot.HardwareAccelerationMethods);

        BlockingError = string.Empty;
    }

    public void ApplyError(string message)
    {
        BlockingError = message;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var snapshot = await _toolchainCapabilitiesService.GetSnapshotAsync();
            ApplySnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ApplyError(ex.Message);
        }
    }

    protected void Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
