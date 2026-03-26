using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class SplitAvViewModel : INotifyPropertyChanged
{
    private readonly IFfmpegService _ffmpegService;
    private readonly IOperationRequestFactory _operationRequestFactory;
    private readonly ICommandBuilder _commandBuilder;

    private string _inputPath = string.Empty;
    private string _audioOutputPath = string.Empty;
    private string _videoOutputPath = string.Empty;
    private string _status = "Idle";
    private bool _useAdvancedCodecSelector;
    private string _selectedAudioCodec = "copy";
    private string _selectedVideoCodec = "copy";
    private string _customAudioCodec = string.Empty;
    private string _customVideoCodec = string.Empty;
    private string _generatedAudioCommand = string.Empty;
    private string _generatedVideoCommand = string.Empty;

    public SplitAvViewModel(
        IFfmpegService ffmpegService,
        IOperationRequestFactory operationRequestFactory,
        ICommandBuilder commandBuilder)
    {
        _ffmpegService = ffmpegService;
        _operationRequestFactory = operationRequestFactory;
        _commandBuilder = commandBuilder;

        AudioCodecPresets = new ObservableCollection<string> { "copy", "aac", "mp3", "flac", "custom" };
        VideoCodecPresets = new ObservableCollection<string> { "copy", "libx264", "libx265", "vp9", "custom" };

        ExtractAudioRequestCommand = new AsyncRelayCommand(ExtractAudioRequestAsync);
        ExtractVideoRequestCommand = new AsyncRelayCommand(ExtractVideoRequestAsync);

        RefreshCommandPreview();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> AudioCodecPresets { get; }

    public ObservableCollection<string> VideoCodecPresets { get; }

    public AsyncRelayCommand ExtractAudioRequestCommand { get; }

    public AsyncRelayCommand ExtractVideoRequestCommand { get; }

    public string InputPath
    {
        get => _inputPath;
        set
        {
            if (Set(ref _inputPath, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public string AudioOutputPath
    {
        get => _audioOutputPath;
        set
        {
            if (Set(ref _audioOutputPath, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public string VideoOutputPath
    {
        get => _videoOutputPath;
        set
        {
            if (Set(ref _videoOutputPath, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public bool UseAdvancedCodecSelector
    {
        get => _useAdvancedCodecSelector;
        set
        {
            if (Set(ref _useAdvancedCodecSelector, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public string SelectedAudioCodec
    {
        get => _selectedAudioCodec;
        set
        {
            if (Set(ref _selectedAudioCodec, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public string SelectedVideoCodec
    {
        get => _selectedVideoCodec;
        set
        {
            if (Set(ref _selectedVideoCodec, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public string CustomAudioCodec
    {
        get => _customAudioCodec;
        set
        {
            if (Set(ref _customAudioCodec, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public string CustomVideoCodec
    {
        get => _customVideoCodec;
        set
        {
            if (Set(ref _customVideoCodec, value))
            {
                RefreshCommandPreview();
            }
        }
    }

    public string GeneratedAudioCommand
    {
        get => _generatedAudioCommand;
        private set => Set(ref _generatedAudioCommand, value);
    }

    public string GeneratedVideoCommand
    {
        get => _generatedVideoCommand;
        private set => Set(ref _generatedVideoCommand, value);
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    private async Task ExtractAudioRequestAsync()
    {
        if (!ValidateForAudioExtraction(out var validationError))
        {
            Status = validationError;
            return;
        }

        try
        {
            Status = "Running ExtractAudioRequest...";
            var exitCode = await ExecuteAsync(OperationKind.ExtractAudio, AudioOutputPath, ResolveAudioCodec());
            Status = exitCode == 0
                ? "ExtractAudioRequest completed."
                : $"ExtractAudioRequest failed with exit code {exitCode}.";
        }
        catch (Exception ex)
        {
            Status = $"ExtractAudioRequest error: {ex.Message}";
        }
    }

    private async Task ExtractVideoRequestAsync()
    {
        if (!ValidateForVideoExtraction(out var validationError))
        {
            Status = validationError;
            return;
        }

        try
        {
            Status = "Running ExtractVideoRequest...";
            var exitCode = await ExecuteAsync(OperationKind.ExtractVideo, VideoOutputPath, ResolveVideoCodec());
            Status = exitCode == 0
                ? "ExtractVideoRequest completed."
                : $"ExtractVideoRequest failed with exit code {exitCode}.";
        }
        catch (Exception ex)
        {
            Status = $"ExtractVideoRequest error: {ex.Message}";
        }
    }

    private Task<int> ExecuteAsync(OperationKind kind, string outputPath, string codec, CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteOperationAsync(
            kind,
            new OperationParameters(
                InputPath,
                outputPath,
                Start: null,
                End: null,
                SubtitleOffset: TimeSpan.Zero,
                SpeedFactor: 1.0,
                AdditionalInputs: [],
                Flags: new Dictionary<string, string>
                {
                    [kind == OperationKind.ExtractAudio ? "audioCodec" : "videoCodec"] = codec
                },
                EncodingProfile: null),
            cancellationToken);

    private bool ValidateForAudioExtraction(out string error)
    {
        return ValidateCommon(out error) && ValidateOutputPath(AudioOutputPath, "audio", out error);
    }

    private bool ValidateForVideoExtraction(out string error)
    {
        return ValidateCommon(out error) && ValidateOutputPath(VideoOutputPath, "video", out error);
    }

    private bool ValidateCommon(out string error)
    {
        var input = InputPath.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Input path is required.";
            return false;
        }

        if (!File.Exists(input))
        {
            error = "Input file does not exist.";
            return false;
        }

        if (!ValidateCodec(ResolveAudioCodec(), "audio", out error) || !ValidateCodec(ResolveVideoCodec(), "video", out error))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool ValidateOutputPath(string outputPath, string streamType, out string error)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            error = $"{streamType} output path is required.";
            return false;
        }

        var normalizedInput = Path.GetFullPath(InputPath.Trim());
        var normalizedOutput = Path.GetFullPath(outputPath.Trim());

        if (string.Equals(normalizedInput, normalizedOutput, StringComparison.OrdinalIgnoreCase))
        {
            error = $"{streamType} output path cannot overwrite input.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(AudioOutputPath) && !string.IsNullOrWhiteSpace(VideoOutputPath))
        {
            var normalizedAudio = Path.GetFullPath(AudioOutputPath.Trim());
            var normalizedVideo = Path.GetFullPath(VideoOutputPath.Trim());
            if (string.Equals(normalizedAudio, normalizedVideo, StringComparison.OrdinalIgnoreCase))
            {
                error = "Audio and video output paths collide.";
                return false;
            }
        }

        var outputDirectory = Path.GetDirectoryName(normalizedOutput);
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            error = $"{streamType} output directory does not exist.";
            return false;
        }

        if (File.Exists(normalizedOutput))
        {
            error = $"{streamType} output file already exists.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool ValidateCodec(string codec, string streamType, out string error)
    {
        if (string.IsNullOrWhiteSpace(codec))
        {
            error = $"{streamType} codec is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private string ResolveAudioCodec()
        => UseAdvancedCodecSelector && SelectedAudioCodec.Equals("custom", StringComparison.OrdinalIgnoreCase)
            ? CustomAudioCodec.Trim()
            : (UseAdvancedCodecSelector ? SelectedAudioCodec : "copy");

    private string ResolveVideoCodec()
        => UseAdvancedCodecSelector && SelectedVideoCodec.Equals("custom", StringComparison.OrdinalIgnoreCase)
            ? CustomVideoCodec.Trim()
            : (UseAdvancedCodecSelector ? SelectedVideoCodec : "copy");

    private void RefreshCommandPreview()
    {
        GeneratedAudioCommand = BuildPreview(OperationKind.ExtractAudio, AudioOutputPath, ResolveAudioCodec());
        GeneratedVideoCommand = BuildPreview(OperationKind.ExtractVideo, VideoOutputPath, ResolveVideoCodec());
    }

    private string BuildPreview(OperationKind kind, string outputPath, string codec)
    {
        if (string.IsNullOrWhiteSpace(InputPath) || string.IsNullOrWhiteSpace(outputPath) || string.IsNullOrWhiteSpace(codec))
        {
            return "Fill input/output and codec to generate command preview.";
        }

        try
        {
            var request = _operationRequestFactory.Create(
                kind,
                new OperationParameters(
                    InputPath.Trim(),
                    outputPath.Trim(),
                    Start: null,
                    End: null,
                    SubtitleOffset: TimeSpan.Zero,
                    SpeedFactor: 1.0,
                    AdditionalInputs: [],
                    Flags: new Dictionary<string, string>
                    {
                        [kind == OperationKind.ExtractAudio ? "audioCodec" : "videoCodec"] = codec
                    },
                    EncodingProfile: null));

            return "ffmpeg " + _commandBuilder.Build(request);
        }
        catch
        {
            return "Unable to generate command with current settings.";
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
        return true;
    }
}
