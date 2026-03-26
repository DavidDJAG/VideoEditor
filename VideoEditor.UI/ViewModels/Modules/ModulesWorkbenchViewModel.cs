using System.ComponentModel;
using System.Runtime.CompilerServices;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class ModulesWorkbenchViewModel : INotifyPropertyChanged
{
    private readonly TrimViewModel _trimViewModel;
    private readonly ConcatViewModel _concatViewModel;
    private readonly TranscodeViewModel _transcodeViewModel;
    private readonly SplitAvViewModel _splitAvViewModel;

    private string _trimInputPath = string.Empty;
    private string _trimOutputPath = string.Empty;
    private TimeSpan _trimStart = TimeSpan.Zero;
    private TimeSpan _trimEnd = TimeSpan.FromSeconds(10);
    private string _trimStatus = "Idle";

    private string _concatInputA = string.Empty;
    private string _concatInputB = string.Empty;
    private string _concatOutputPath = string.Empty;
    private string _concatStatus = "Idle";

    private string _splitInputPath = string.Empty;
    private string _audioOutputPath = string.Empty;
    private string _videoOutputPath = string.Empty;
    private string _splitStatus = "Idle";

    private string _convertInputPath = string.Empty;
    private string _convertOutputPath = string.Empty;
    private string _convertStatus = "Idle";

    public ModulesWorkbenchViewModel(
        TrimViewModel trimViewModel,
        ConcatViewModel concatViewModel,
        TranscodeViewModel transcodeViewModel,
        SplitAvViewModel splitAvViewModel)
    {
        _trimViewModel = trimViewModel;
        _concatViewModel = concatViewModel;
        _transcodeViewModel = transcodeViewModel;
        _splitAvViewModel = splitAvViewModel;

        RunTrimCommand = new AsyncRelayCommand(RunTrimAsync);
        RunConcatCommand = new AsyncRelayCommand(RunConcatAsync);
        RunSplitAvCommand = new AsyncRelayCommand(RunSplitAvAsync);
        RunConvertCommand = new AsyncRelayCommand(RunConvertAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AsyncRelayCommand RunTrimCommand { get; }

    public AsyncRelayCommand RunConcatCommand { get; }

    public AsyncRelayCommand RunSplitAvCommand { get; }

    public AsyncRelayCommand RunConvertCommand { get; }

    public string TrimInputPath { get => _trimInputPath; set => Set(ref _trimInputPath, value); }
    public string TrimOutputPath { get => _trimOutputPath; set => Set(ref _trimOutputPath, value); }
    public TimeSpan TrimStart { get => _trimStart; set => Set(ref _trimStart, value); }
    public TimeSpan TrimEnd { get => _trimEnd; set => Set(ref _trimEnd, value); }
    public string TrimStatus { get => _trimStatus; private set => Set(ref _trimStatus, value); }

    public string ConcatInputA { get => _concatInputA; set => Set(ref _concatInputA, value); }
    public string ConcatInputB { get => _concatInputB; set => Set(ref _concatInputB, value); }
    public string ConcatOutputPath { get => _concatOutputPath; set => Set(ref _concatOutputPath, value); }
    public string ConcatStatus { get => _concatStatus; private set => Set(ref _concatStatus, value); }

    public string SplitInputPath { get => _splitInputPath; set => Set(ref _splitInputPath, value); }
    public string AudioOutputPath { get => _audioOutputPath; set => Set(ref _audioOutputPath, value); }
    public string VideoOutputPath { get => _videoOutputPath; set => Set(ref _videoOutputPath, value); }
    public string SplitStatus { get => _splitStatus; private set => Set(ref _splitStatus, value); }

    public string ConvertInputPath { get => _convertInputPath; set => Set(ref _convertInputPath, value); }
    public string ConvertOutputPath { get => _convertOutputPath; set => Set(ref _convertOutputPath, value); }
    public string ConvertStatus { get => _convertStatus; private set => Set(ref _convertStatus, value); }

    private async Task RunTrimAsync()
    {
        if (string.IsNullOrWhiteSpace(TrimInputPath) || string.IsNullOrWhiteSpace(TrimOutputPath) || TrimEnd <= TrimStart)
        {
            TrimStatus = "Invalid trim parameters.";
            return;
        }

        try
        {
            TrimStatus = "Running trim...";
            var exitCode = await _trimViewModel.ExecuteAsync(new OperationParameters(
                TrimInputPath,
                TrimOutputPath,
                TrimStart,
                TrimEnd,
                SubtitleOffset: TimeSpan.Zero,
                SpeedFactor: 1.0,
                AdditionalInputs: [],
                Flags: new Dictionary<string, string>(),
                EncodingProfile: null));
            TrimStatus = exitCode == 0 ? "Trim completed." : $"Trim failed with exit code {exitCode}.";
        }
        catch (Exception ex)
        {
            TrimStatus = $"Trim error: {ex.Message}";
        }
    }

    private async Task RunConcatAsync()
    {
        if (string.IsNullOrWhiteSpace(ConcatInputA) || string.IsNullOrWhiteSpace(ConcatInputB) || string.IsNullOrWhiteSpace(ConcatOutputPath))
        {
            ConcatStatus = "Invalid concat parameters.";
            return;
        }

        try
        {
            ConcatStatus = "Running concat...";
            var exitCode = await _concatViewModel.ExecuteAsync(new OperationParameters(
                InputPath: ConcatInputA,
                OutputPath: ConcatOutputPath,
                Start: null,
                End: null,
                SubtitleOffset: TimeSpan.Zero,
                SpeedFactor: 1.0,
                AdditionalInputs: [],
                Flags: new Dictionary<string, string>(),
                EncodingProfile: null,
                ConcatInputs: [ConcatInputA, ConcatInputB]));
            ConcatStatus = exitCode == 0 ? "Concat completed." : $"Concat failed with exit code {exitCode}.";
        }
        catch (Exception ex)
        {
            ConcatStatus = $"Concat error: {ex.Message}";
        }
    }

    private async Task RunSplitAvAsync()
    {
        if (string.IsNullOrWhiteSpace(SplitInputPath) || string.IsNullOrWhiteSpace(AudioOutputPath) || string.IsNullOrWhiteSpace(VideoOutputPath))
        {
            SplitStatus = "Invalid split A/V parameters.";
            return;
        }

        try
        {
            SplitStatus = "Extracting audio...";
            var audioExitCode = await _splitAvViewModel.ExtractAudioAsync(SplitInputPath, AudioOutputPath);
            if (audioExitCode != 0)
            {
                SplitStatus = $"Audio extraction failed with exit code {audioExitCode}.";
                return;
            }

            SplitStatus = "Extracting video...";
            var videoExitCode = await _splitAvViewModel.ExtractVideoAsync(SplitInputPath, VideoOutputPath);
            SplitStatus = videoExitCode == 0 ? "Split A/V completed." : $"Video extraction failed with exit code {videoExitCode}.";
        }
        catch (Exception ex)
        {
            SplitStatus = $"Split A/V error: {ex.Message}";
        }
    }

    private async Task RunConvertAsync()
    {
        if (string.IsNullOrWhiteSpace(ConvertInputPath) || string.IsNullOrWhiteSpace(ConvertOutputPath))
        {
            ConvertStatus = "Invalid convert parameters.";
            return;
        }

        try
        {
            ConvertStatus = "Running convert...";
            var profile = new EncodingProfile(
                Name: "Balanced H264/AAC",
                VideoCodec: "libx264",
                AudioCodec: "aac",
                Container: "mp4",
                VideoBitrate: "2500k",
                AudioBitrate: "160k",
                PixelFormat: "yuv420p",
                Preset: "medium");

            var exitCode = await _transcodeViewModel.ExecuteAsync(new OperationParameters(
                ConvertInputPath,
                ConvertOutputPath,
                Start: null,
                End: null,
                SubtitleOffset: TimeSpan.Zero,
                SpeedFactor: 1.0,
                AdditionalInputs: [],
                Flags: new Dictionary<string, string>(),
                EncodingProfile: profile));

            ConvertStatus = exitCode == 0 ? "Convert completed." : $"Convert failed with exit code {exitCode}.";
        }
        catch (Exception ex)
        {
            ConvertStatus = $"Convert error: {ex.Message}";
        }
    }

    private void Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
