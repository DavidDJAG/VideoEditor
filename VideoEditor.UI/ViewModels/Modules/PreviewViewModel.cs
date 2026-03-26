using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Win32;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class PreviewViewModel : INotifyPropertyChanged
{
    private readonly IFfprobeService _ffprobeService;
    private readonly IPlaybackService _playbackService;

    private string _inputPath = string.Empty;
    private string _status = "Idle";
    private string _probeSummary = "Run Probe to inspect the selected media file.";
    private string _probeJson = "No probe data loaded.";
    private TimeSpan? _inMarker;
    private TimeSpan? _outMarker;
    private TimeSpan? _aStart;
    private TimeSpan? _aEnd;
    private TimeSpan? _bStart;
    private TimeSpan? _bEnd;
    private TimeSpan? _selectedSeek;
    private TimeSpan _subtitleOffset = TimeSpan.Zero;
    private double _speedFactor = 1.0;

    public PreviewViewModel(IFfprobeService ffprobeService, IPlaybackService playbackService)
    {
        _ffprobeService = ffprobeService;
        _playbackService = playbackService;

        BrowseInputCommand = new AsyncRelayCommand(BrowseInputAsync);
        LoadProbeCommand = new AsyncRelayCommand(LoadProbeAsync, CanLoadInput);
        PlaySelectionCommand = new AsyncRelayCommand(PlaySelectionAsync, CanLoadInput);
        PlayAbCommand = new AsyncRelayCommand(PlayAbAsync, CanLoadInput);
        QuickSeekCommand = new AsyncRelayCommand(QuickSeekAsync, () => CanLoadInput() && SelectedSeek.HasValue);
        StopCommand = new AsyncRelayCommand(StopAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TimeSpan> ProbeSeekPoints { get; } = [];

    public AsyncRelayCommand BrowseInputCommand { get; }

    public AsyncRelayCommand LoadProbeCommand { get; }

    public AsyncRelayCommand PlaySelectionCommand { get; }

    public AsyncRelayCommand PlayAbCommand { get; }

    public AsyncRelayCommand QuickSeekCommand { get; }

    public AsyncRelayCommand StopCommand { get; }

    public string InputPath
    {
        get => _inputPath;
        set => Set(ref _inputPath, value);
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public string ProbeSummary
    {
        get => _probeSummary;
        private set => Set(ref _probeSummary, value);
    }

    public string ProbeJson
    {
        get => _probeJson;
        private set => Set(ref _probeJson, value);
    }

    public TimeSpan? InMarker
    {
        get => _inMarker;
        set => Set(ref _inMarker, value);
    }

    public TimeSpan? OutMarker
    {
        get => _outMarker;
        set => Set(ref _outMarker, value);
    }

    public TimeSpan? AStart
    {
        get => _aStart;
        set => Set(ref _aStart, value);
    }

    public TimeSpan? AEnd
    {
        get => _aEnd;
        set => Set(ref _aEnd, value);
    }

    public TimeSpan? BStart
    {
        get => _bStart;
        set => Set(ref _bStart, value);
    }

    public TimeSpan? BEnd
    {
        get => _bEnd;
        set => Set(ref _bEnd, value);
    }

    public TimeSpan SubtitleOffset
    {
        get => _subtitleOffset;
        set => Set(ref _subtitleOffset, value);
    }

    public double SpeedFactor
    {
        get => _speedFactor;
        set => Set(ref _speedFactor, value);
    }

    public TimeSpan? SelectedSeek
    {
        get => _selectedSeek;
        set => Set(ref _selectedSeek, value);
    }

    public OperationParameters BuildLinkedParameters()
        => new(
            InputPath,
            OutputPath: null,
            Start: InMarker,
            End: OutMarker,
            SubtitleOffset,
            SpeedFactor,
            AdditionalInputs: [],
            Flags: new Dictionary<string, string>(),
            EncodingProfile: null,
            ConcatInputs: null);

    private bool CanLoadInput()
        => !string.IsNullOrWhiteSpace(InputPath) && File.Exists(InputPath);

    private Task BrowseInputAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select media file",
            CheckFileExists = true,
            Filter = "Media files|*.mp4;*.mkv;*.mov;*.avi;*.wmv;*.mp3;*.wav;*.m4a;*.flac;*.srt;*.ass;*.vtt|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            InputPath = dialog.FileName;
            Status = "Input file selected";
        }

        return Task.CompletedTask;
    }

    private async Task LoadProbeAsync()
    {
        try
        {
            Status = "Loading probe...";
            var result = await _ffprobeService.ProbeAsync(InputPath);
            PopulateSeekPoints(result);
            PopulateProbeDetails(result);
            if (!InMarker.HasValue)
            {
                InMarker = TimeSpan.Zero;
            }

            if (!OutMarker.HasValue && result.Duration > TimeSpan.Zero)
            {
                OutMarker = result.Duration;
            }

            Status = $"Probe loaded ({ProbeSeekPoints.Count} seek points)";
        }
        catch (Exception ex)
        {
            ProbeSummary = "Probe failed.";
            ProbeJson = ex.Message;
            Status = $"Probe failed: {ex.Message}";
        }
    }

    private async Task PlaySelectionAsync()
    {
        try
        {
            Status = "Preview playing...";
            await _playbackService.PlayAsync(InputPath, InMarker, OutMarker, SpeedFactor, SubtitleOffset);
            Status = "Preview started";
        }
        catch (Exception ex)
        {
            Status = $"Preview failed: {_playbackService.LastError ?? ex.Message}";
        }
    }

    private async Task PlayAbAsync()
    {
        try
        {
            Status = "A/B preview playing...";
            await _playbackService.PlayABPreviewAsync(InputPath, AStart, AEnd, BStart, BEnd, SpeedFactor, SubtitleOffset);
            Status = "A/B preview started";
        }
        catch (Exception ex)
        {
            Status = $"A/B preview failed: {_playbackService.LastError ?? ex.Message}";
        }
    }

    private async Task QuickSeekAsync()
    {
        if (!SelectedSeek.HasValue)
        {
            return;
        }

        try
        {
            Status = $"Seeking to {SelectedSeek.Value:c}...";
            await _playbackService.PlayAsync(InputPath, SelectedSeek, OutMarker, SpeedFactor, SubtitleOffset);
            Status = "Seek preview started";
        }
        catch (Exception ex)
        {
            Status = $"Seek failed: {_playbackService.LastError ?? ex.Message}";
        }
    }

    private async Task StopAsync()
    {
        await _playbackService.StopAsync();
        Status = "Stopped";
    }

    private void PopulateSeekPoints(MediaProbeResult result)
    {
        ProbeSeekPoints.Clear();

        if (result.Duration <= TimeSpan.Zero)
        {
            ProbeSeekPoints.Add(TimeSpan.Zero);
            return;
        }

        var slices = 8;
        var step = TimeSpan.FromTicks(result.Duration.Ticks / slices);
        for (var i = 0; i <= slices; i++)
        {
            ProbeSeekPoints.Add(step * i);
        }
    }

    private bool Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        RefreshCommands(propertyName);
        return true;
    }

    private void RefreshCommands(string? propertyName)
    {
        if (propertyName is nameof(InputPath) or nameof(SelectedSeek))
        {
            LoadProbeCommand.NotifyCanExecuteChanged();
            PlaySelectionCommand.NotifyCanExecuteChanged();
            PlayAbCommand.NotifyCanExecuteChanged();
            QuickSeekCommand.NotifyCanExecuteChanged();
        }
    }

    private void PopulateProbeDetails(MediaProbeResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"File: {result.FilePath}");
        builder.AppendLine($"Container: {result.Container}");
        builder.AppendLine($"Duration: {result.Duration:c}");
        builder.AppendLine($"Size: {result.SizeBytes:N0} bytes");
        builder.AppendLine($"Video streams: {result.VideoStreamCount}");
        builder.AppendLine($"Audio streams: {result.AudioStreamCount}");
        builder.AppendLine($"Subtitle streams: {result.SubtitleStreamCount}");

        if (result.Width.HasValue && result.Height.HasValue)
        {
            builder.AppendLine($"Resolution: {result.Width}x{result.Height}");
        }

        if (result.FrameRate.HasValue)
        {
            builder.AppendLine($"Frame rate: {result.FrameRate.Value:0.###} fps");
        }

        if (result.AudioSampleRate.HasValue)
        {
            builder.AppendLine($"Sample rate: {result.AudioSampleRate.Value} Hz");
        }

        if (result.AudioChannels.HasValue)
        {
            builder.AppendLine($"Audio channels: {result.AudioChannels.Value}");
        }

        ProbeSummary = builder.ToString().TrimEnd();
        ProbeJson = result.RawJson;
    }
}
