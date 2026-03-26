using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class ModulesWorkbenchViewModel : INotifyPropertyChanged
{
    private readonly TrimViewModel _trimViewModel;
    private readonly ConcatViewModel _concatViewModel;
    private readonly TranscodeViewModel _transcodeViewModel;

    private string _trimInputPath = string.Empty;
    private string _trimOutputPath = string.Empty;
    private TimeSpan _trimStart = TimeSpan.Zero;
    private TimeSpan _trimEnd = TimeSpan.FromSeconds(10);
    private string _trimStatus = "Idle";

    private string _concatInputA = string.Empty;
    private string _concatInputB = string.Empty;
    private string _concatOutputPath = string.Empty;
    private string _concatStatus = "Idle";

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
        SplitAvViewModel = splitAvViewModel;

        RunTrimCommand = new AsyncRelayCommand(RunTrimAsync);
        RunConcatCommand = new AsyncRelayCommand(RunConcatAsync);
        RunConvertCommand = new AsyncRelayCommand(RunConvertAsync);
        OpenTrimInputCommand = new AsyncRelayCommand(OpenTrimInputAsync);
        SaveTrimOutputCommand = new AsyncRelayCommand(SaveTrimOutputAsync);
        OpenConcatInputACommand = new AsyncRelayCommand(OpenConcatInputAAsync);
        OpenConcatInputBCommand = new AsyncRelayCommand(OpenConcatInputBAsync);
        SaveConcatOutputCommand = new AsyncRelayCommand(SaveConcatOutputAsync);
        OpenConvertInputCommand = new AsyncRelayCommand(OpenConvertInputAsync);
        SaveConvertOutputCommand = new AsyncRelayCommand(SaveConvertOutputAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AsyncRelayCommand RunTrimCommand { get; }

    public AsyncRelayCommand RunConcatCommand { get; }

    public AsyncRelayCommand RunConvertCommand { get; }

    public AsyncRelayCommand OpenTrimInputCommand { get; }

    public AsyncRelayCommand SaveTrimOutputCommand { get; }

    public AsyncRelayCommand OpenConcatInputACommand { get; }

    public AsyncRelayCommand OpenConcatInputBCommand { get; }

    public AsyncRelayCommand SaveConcatOutputCommand { get; }

    public AsyncRelayCommand OpenConvertInputCommand { get; }

    public AsyncRelayCommand SaveConvertOutputCommand { get; }

    public SplitAvViewModel SplitAvViewModel { get; }

    public string TrimInputPath { get => _trimInputPath; set => Set(ref _trimInputPath, value); }
    public string TrimOutputPath { get => _trimOutputPath; set => Set(ref _trimOutputPath, value); }
    public TimeSpan TrimStart { get => _trimStart; set => Set(ref _trimStart, value); }
    public TimeSpan TrimEnd { get => _trimEnd; set => Set(ref _trimEnd, value); }
    public string TrimStatus { get => _trimStatus; private set => Set(ref _trimStatus, value); }

    public string ConcatInputA { get => _concatInputA; set => Set(ref _concatInputA, value); }
    public string ConcatInputB { get => _concatInputB; set => Set(ref _concatInputB, value); }
    public string ConcatOutputPath { get => _concatOutputPath; set => Set(ref _concatOutputPath, value); }
    public string ConcatStatus { get => _concatStatus; private set => Set(ref _concatStatus, value); }

    public string ConvertInputPath { get => _convertInputPath; set => Set(ref _convertInputPath, value); }
    public string ConvertOutputPath { get => _convertOutputPath; set => Set(ref _convertOutputPath, value); }
    public string ConvertStatus { get => _convertStatus; private set => Set(ref _convertStatus, value); }

    private Task OpenTrimInputAsync()
    {
        if (TryOpenFile("Select trim input file", out var filePath))
        {
            TrimInputPath = filePath;
        }

        return Task.CompletedTask;
    }

    private Task SaveTrimOutputAsync()
    {
        if (TrySaveFile("Select trim output file", TrimOutputPath, TrimInputPath, out var filePath))
        {
            TrimOutputPath = filePath;
        }

        return Task.CompletedTask;
    }

    private Task OpenConcatInputAAsync()
    {
        if (TryOpenFile("Select concat input A", out var filePath))
        {
            ConcatInputA = filePath;
        }

        return Task.CompletedTask;
    }

    private Task OpenConcatInputBAsync()
    {
        if (TryOpenFile("Select concat input B", out var filePath))
        {
            ConcatInputB = filePath;
        }

        return Task.CompletedTask;
    }

    private Task SaveConcatOutputAsync()
    {
        if (TrySaveFile("Select concat output file", ConcatOutputPath, ConcatInputA, out var filePath))
        {
            ConcatOutputPath = filePath;
        }

        return Task.CompletedTask;
    }

    private Task OpenConvertInputAsync()
    {
        if (TryOpenFile("Select convert input file", out var filePath))
        {
            ConvertInputPath = filePath;
        }

        return Task.CompletedTask;
    }

    private Task SaveConvertOutputAsync()
    {
        if (TrySaveFile("Select converted output file", ConvertOutputPath, ConvertInputPath, out var filePath))
        {
            ConvertOutputPath = filePath;
        }

        return Task.CompletedTask;
    }

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

    private static bool TryOpenFile(string title, out string filePath)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            CheckFileExists = true,
            Filter = MediaFileFilter
        };

        if (dialog.ShowDialog() == true)
        {
            filePath = dialog.FileName;
            return true;
        }

        filePath = string.Empty;
        return false;
    }

    private static bool TrySaveFile(string title, string currentPath, string sourcePath, out string filePath)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = MediaFileFilter,
            FileName = GetSuggestedFileName(currentPath, sourcePath)
        };

        if (dialog.ShowDialog() == true)
        {
            filePath = dialog.FileName;
            return true;
        }

        filePath = string.Empty;
        return false;
    }

    private static string GetSuggestedFileName(string currentPath, string sourcePath)
    {
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            return Path.GetFileName(currentPath);
        }

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            return Path.GetFileName(sourcePath);
        }

        return string.Empty;
    }

    private const string MediaFileFilter = "Media files|*.mp4;*.mkv;*.mov;*.avi;*.wmv;*.mp3;*.wav;*.m4a;*.flac;*.aac;*.ts|All files|*.*";
}
