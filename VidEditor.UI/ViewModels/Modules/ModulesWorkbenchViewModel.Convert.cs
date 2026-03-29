using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using VidEditor.Application.Abstractions;
using VidEditor.Application.Services;
using VidEditor.Domain.Models;

namespace VidEditor.UI.ViewModels.Modules;

public sealed partial class ModulesWorkbenchViewModel
{
    private static readonly string[] DefaultConvertContainerIds = ContainerCatalog.GetUserSelectableContainers().Select(static container => container.Id).ToArray();
    private static readonly string[] DefaultConvertVideoCodecOptions = ["libx264", "libx265", "libsvtav1", "libaom-av1", "libvpx-vp9", "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_qsv", "hevc_qsv", "h264_amf", "hevc_amf", "h264_vaapi", "hevc_vaapi", "ffv1", "prores_ks", "mjpeg"];
    private static readonly string[] DefaultConvertAudioCodecOptions = ["aac", "libopus", "libmp3lame", "flac", "ac3", "alac", "pcm_s16le", "pcm_s24le", "libvorbis", "wavpack"];
    private static readonly string[] DefaultConvertSubtitleCodecOptions = ["copy", "mov_text", "srt", "ass", "webvtt"];
    private static readonly string[] DefaultConvertPixelFormatOptions = [string.Empty, "yuv420p", "yuv422p", "yuv444p", "nv12", "p010le", "rgb24", "yuv420p10le"];
    private static readonly string[] WebmCompatibleVideoEncoders = ["libvpx", "libvpx-vp9", "vp8", "vp9", "libaom-av1", "libsvtav1", "rav1e", "av1_nvenc", "av1_qsv", "av1_amf", "av1_vaapi"];
    private static readonly string[] WebmCompatibleAudioEncoders = ["libopus", "opus", "libvorbis", "vorbis"];
    private static readonly HashSet<string> ContainersWithLimitedOpusSupport = new(StringComparer.OrdinalIgnoreCase) { "mp4", "mov", "m4a", "m4v" };
    private static readonly HashSet<string> MatroskaFriendlyContainers = new(StringComparer.OrdinalIgnoreCase) { "mkv", "webm" };
    private static readonly JsonSerializerOptions ConvertPresetJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static readonly HashSet<string> ConvertEditorPropertyNames = new(StringComparer.Ordinal)
    {
        nameof(ConvertInputPath),
        nameof(ConvertOutputPath),
        nameof(ConvertContainer),
        nameof(ConvertOverwriteExisting),
        nameof(ConvertFastStart),
        nameof(ConvertUseHardwareAcceleration),
        nameof(ConvertShowAdvancedOptions),
        nameof(ConvertVideoMode),
        nameof(ConvertVideoCodec),
        nameof(ConvertVideoRateControlMode),
        nameof(ConvertVideoCrfText),
        nameof(ConvertVideoBitrate),
        nameof(ConvertVideoPreset),
        nameof(ConvertVideoTune),
        nameof(ConvertVideoPixelFormat),
        nameof(ConvertVideoFrameRateMode),
        nameof(ConvertVideoFrameRateText),
        nameof(ConvertVideoScaleMode),
        nameof(ConvertVideoWidthText),
        nameof(ConvertVideoHeightText),
        nameof(ConvertVideoProfile),
        nameof(ConvertVideoLevel),
        nameof(ConvertVideoGopText),
        nameof(ConvertVideoSourceStreamIndex),
        nameof(ConvertVideoPassMode),
        nameof(ConvertVideoDeinterlaceMode),
        nameof(ConvertVideoCropXText),
        nameof(ConvertVideoCropYText),
        nameof(ConvertVideoCropWidthText),
        nameof(ConvertVideoCropHeightText),
        nameof(ConvertVideoPadEnabled),
        nameof(ConvertVideoPadWidthText),
        nameof(ConvertVideoPadHeightText),
        nameof(ConvertVideoPadXText),
        nameof(ConvertVideoPadYText),
        nameof(ConvertAudioMode),
        nameof(ConvertAudioCodec),
        nameof(ConvertAudioBitrate),
        nameof(ConvertAudioSampleRateText),
        nameof(ConvertAudioChannelsText),
        nameof(ConvertAudioChannelLayout),
        nameof(ConvertAudioSourceStreamIndex),
        nameof(ConvertAudioAdditionalStreamIndexesText),
        nameof(ConvertSubtitleMode),
        nameof(ConvertSubtitleSourceStreamIndex),
        nameof(ConvertSubtitleAdditionalStreamIndexesText),
        nameof(ConvertSubtitleCodec),
        nameof(ConvertSubtitleLanguage),
        nameof(ConvertSubtitleSetAsDefault),
        nameof(ConvertMetadataCopySourceMetadata),
        nameof(ConvertMetadataCopyChapters),
        nameof(ConvertMetadataTitle),
        nameof(ConvertMetadataArtist),
        nameof(ConvertMetadataComment),
        nameof(ConvertOutputNamingTemplate),
        nameof(ConvertAudioNormalizationMode),
        nameof(ConvertAudioLoudnessTargetText),
        nameof(ConvertAudioTruePeakText),
        nameof(ConvertAudioLoudnessRangeText)
    };

    private static readonly HashSet<string> ConvertPresetMutationPropertyNames = new(StringComparer.Ordinal)
    {
        nameof(ConvertContainer),
        nameof(ConvertOverwriteExisting),
        nameof(ConvertFastStart),
        nameof(ConvertUseHardwareAcceleration),
        nameof(ConvertShowAdvancedOptions),
        nameof(ConvertVideoMode),
        nameof(ConvertVideoCodec),
        nameof(ConvertVideoRateControlMode),
        nameof(ConvertVideoCrfText),
        nameof(ConvertVideoBitrate),
        nameof(ConvertVideoPreset),
        nameof(ConvertVideoTune),
        nameof(ConvertVideoPixelFormat),
        nameof(ConvertVideoFrameRateMode),
        nameof(ConvertVideoFrameRateText),
        nameof(ConvertVideoScaleMode),
        nameof(ConvertVideoWidthText),
        nameof(ConvertVideoHeightText),
        nameof(ConvertVideoProfile),
        nameof(ConvertVideoLevel),
        nameof(ConvertVideoGopText),
        nameof(ConvertVideoSourceStreamIndex),
        nameof(ConvertVideoPassMode),
        nameof(ConvertVideoDeinterlaceMode),
        nameof(ConvertVideoCropXText),
        nameof(ConvertVideoCropYText),
        nameof(ConvertVideoCropWidthText),
        nameof(ConvertVideoCropHeightText),
        nameof(ConvertVideoPadEnabled),
        nameof(ConvertVideoPadWidthText),
        nameof(ConvertVideoPadHeightText),
        nameof(ConvertVideoPadXText),
        nameof(ConvertVideoPadYText),
        nameof(ConvertAudioMode),
        nameof(ConvertAudioCodec),
        nameof(ConvertAudioBitrate),
        nameof(ConvertAudioSampleRateText),
        nameof(ConvertAudioChannelsText),
        nameof(ConvertAudioChannelLayout),
        nameof(ConvertAudioSourceStreamIndex),
        nameof(ConvertAudioAdditionalStreamIndexesText),
        nameof(ConvertSubtitleMode),
        nameof(ConvertSubtitleSourceStreamIndex),
        nameof(ConvertSubtitleAdditionalStreamIndexesText),
        nameof(ConvertSubtitleCodec),
        nameof(ConvertSubtitleLanguage),
        nameof(ConvertSubtitleSetAsDefault),
        nameof(ConvertMetadataCopySourceMetadata),
        nameof(ConvertMetadataCopyChapters),
        nameof(ConvertMetadataTitle),
        nameof(ConvertMetadataArtist),
        nameof(ConvertMetadataComment),
        nameof(ConvertAudioNormalizationMode),
        nameof(ConvertAudioLoudnessTargetText),
        nameof(ConvertAudioTruePeakText),
        nameof(ConvertAudioLoudnessRangeText)
    };

    private static readonly HashSet<string> AudioOnlyContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        "aac",
        "flac",
        "m4a",
        "mp3",
        "ogg",
        "opus",
        "wav"
    };

    private readonly Dictionary<string, ConvertOptions> _convertBuiltInPresets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConvertPresetRecord> _convertUserPresets = new(StringComparer.OrdinalIgnoreCase);

    private ToolchainCapabilitiesSnapshot? _convertCapabilitiesSnapshot;
    private MediaProbeResult? _convertInputProbeResult;
    private CancellationTokenSource? _convertInputProbeCancellationSource;
    private bool _isApplyingConvertPreset;
    private bool _isApplyingConvertCapabilities;
    private bool _convertCapabilitiesInitialized;
    private bool _convertInputContextInitialized;
    private int _convertInputProbeRequestId;

    private bool _convertShowAdvancedOptions = true;
    private string _convertPresetName = "Balanced H.264 MP4";
    private string _convertSelectedPresetName = "Balanced H.264 MP4";
    private string _convertPresetDraftName = "Balanced H.264 MP4";
    private string _convertPresetLibraryStatus = "Built-in presets are ready. Save your own presets to persist convert recipes for this installation.";
    private string _convertAdaptivePresetSummary = "Adaptive preset guidance will appear after capability analysis completes.";
    private string _convertOptimizationSummary = "Current optimization hints will appear here once the convert profile is evaluated.";
    private string _convertQueueSummary = "Queue integration is ready. Add validated convert jobs to the processing queue for sequential execution.";
    private string _convertQueueSnapshotSummary = "Queue snapshot unavailable until the first queue refresh completes.";
    private string _convertLastQueueJobSummary = "No convert job has been submitted yet from the Convert tab.";
    private string _convertQueueHistoryStatus = "Queue history is empty for Convert jobs right now.";
    private string _convertCommandPreview = string.Empty;
    private string _convertValidationSummary = string.Empty;
    private string _convertAdvisorySummary = string.Empty;
    private string _convertCapabilitiesStatus = "Using built-in codec options until FFmpeg capabilities are loaded.";
    private string _convertHardwareAccelerationSummary = "No hardware acceleration data available.";
    private string _convertInputProbeStatus = "Select an input file to inspect it with ffprobe.";
    private string _convertInputSourceSummary = "No input metadata loaded.";
    private string _convertInputVideoSummary = "Video: —";
    private string _convertInputAudioSummary = "Audio: —";
    private string _convertInputStreamsSummary = "Streams: —";
    private string _convertInputRecommendationSummary = "ffprobe context will enable smarter validation for Copy, audio-only sources and container compatibility.";
    private bool _convertIsInspectingInput;

    private string _convertContainer = "mp4";
    private bool _convertOverwriteExisting = true;
    private bool _convertFastStart = true;
    private bool _convertUseHardwareAcceleration;

    private StreamProcessingMode _convertVideoMode = StreamProcessingMode.Encode;
    private string _convertVideoCodec = "libx264";
    private VideoRateControlMode _convertVideoRateControlMode = VideoRateControlMode.Bitrate;
    private string _convertVideoCrfText = string.Empty;
    private string _convertVideoBitrate = "2500k";
    private string _convertVideoPreset = "medium";
    private string _convertVideoTune = string.Empty;
    private string _convertVideoPixelFormat = "yuv420p";
    private FrameRateMode _convertVideoFrameRateMode = FrameRateMode.KeepSource;
    private string _convertVideoFrameRateText = string.Empty;
    private ScaleMode _convertVideoScaleMode = ScaleMode.KeepSource;
    private string _convertVideoWidthText = string.Empty;
    private string _convertVideoHeightText = string.Empty;
    private string _convertVideoProfile = string.Empty;
    private string _convertVideoLevel = string.Empty;
    private string _convertVideoGopText = string.Empty;
    private VideoPassMode _convertVideoPassMode = VideoPassMode.SinglePass;
    private VideoDeinterlaceMode _convertVideoDeinterlaceMode = VideoDeinterlaceMode.Off;
    private string _convertVideoCropXText = string.Empty;
    private string _convertVideoCropYText = string.Empty;
    private string _convertVideoCropWidthText = string.Empty;
    private string _convertVideoCropHeightText = string.Empty;
    private bool _convertVideoPadEnabled;
    private string _convertVideoPadWidthText = string.Empty;
    private string _convertVideoPadHeightText = string.Empty;
    private string _convertVideoPadXText = string.Empty;
    private string _convertVideoPadYText = string.Empty;

    private StreamProcessingMode _convertAudioMode = StreamProcessingMode.Encode;
    private string _convertAudioCodec = "aac";
    private string _convertAudioBitrate = "160k";
    private string _convertAudioSampleRateText = string.Empty;
    private string _convertAudioChannelsText = string.Empty;
    private string _convertAudioChannelLayout = string.Empty;
    private AudioNormalizationMode _convertAudioNormalizationMode = AudioNormalizationMode.None;
    private string _convertAudioLoudnessTargetText = "-16";
    private string _convertAudioTruePeakText = "-1.5";
    private string _convertAudioLoudnessRangeText = "11";

    private int? _convertVideoSourceStreamIndex;
    private int? _convertAudioSourceStreamIndex;
    private string _convertAudioAdditionalStreamIndexesText = string.Empty;
    private SubtitleProcessingMode _convertSubtitleMode = SubtitleProcessingMode.Disable;
    private int? _convertSubtitleSourceStreamIndex;
    private string _convertSubtitleAdditionalStreamIndexesText = string.Empty;
    private string _convertSubtitleCodec = string.Empty;
    private string _convertSubtitleLanguage = string.Empty;
    private bool _convertSubtitleSetAsDefault;
    private bool _convertMetadataCopySourceMetadata = true;
    private bool _convertMetadataCopyChapters = true;
    private string _convertMetadataTitle = string.Empty;
    private string _convertMetadataArtist = string.Empty;
    private string _convertMetadataComment = string.Empty;
    private string _convertOutputNamingTemplate = "{name}_{vcodec}_{container}";
    private string _convertOutputNamePreview = "Select an input to preview the generated output name.";
    private string _convertBatchSummary = "Batch convert is idle. Add multiple input files to enqueue a whole set from the Convert tab.";

    public ObservableCollection<ContainerDefinition> ConvertContainerOptions { get; } = new();
    public ObservableCollection<string> ConvertPresetLibraryNames { get; } = new();
    public ObservableCollection<ConvertQueueHistoryItem> ConvertRecentQueueItems { get; } = new();
    public ObservableCollection<ConvertSourceStreamOption> ConvertVideoStreamOptions { get; } = new();
    public ObservableCollection<ConvertSourceStreamOption> ConvertAudioStreamOptions { get; } = new();
    public ObservableCollection<ConvertSourceStreamOption> ConvertSubtitleStreamOptions { get; } = new();
    public ObservableCollection<string> ConvertSubtitleCodecOptions { get; } = new();
    public ObservableCollection<string> ConvertBatchInputPaths { get; } = new();
    public IReadOnlyList<StreamProcessingMode> ConvertStreamProcessingModes { get; } = Enum.GetValues<StreamProcessingMode>();
    public IReadOnlyList<VideoRateControlMode> ConvertVideoRateControlModes { get; } = Enum.GetValues<VideoRateControlMode>();
    public IReadOnlyList<FrameRateMode> ConvertFrameRateModes { get; } = Enum.GetValues<FrameRateMode>();
    public IReadOnlyList<ScaleMode> ConvertScaleModes { get; } = Enum.GetValues<ScaleMode>();
    public IReadOnlyList<VideoPassMode> ConvertVideoPassModes { get; } = Enum.GetValues<VideoPassMode>();
    public IReadOnlyList<VideoDeinterlaceMode> ConvertVideoDeinterlaceModes { get; } = Enum.GetValues<VideoDeinterlaceMode>();
    public IReadOnlyList<AudioNormalizationMode> ConvertAudioNormalizationModes { get; } = Enum.GetValues<AudioNormalizationMode>();
    public IReadOnlyList<SubtitleProcessingMode> ConvertSubtitleProcessingModes { get; } = Enum.GetValues<SubtitleProcessingMode>();
    public ObservableCollection<string> ConvertVideoCodecOptions { get; } = new();
    public ObservableCollection<string> ConvertAudioCodecOptions { get; } = new();
    public IReadOnlyList<string> ConvertVideoPresetOptions { get; } = ["ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"];
    public IReadOnlyList<string> ConvertVideoTuneOptions { get; } = [string.Empty, "film", "animation", "grain", "stillimage", "fastdecode", "zerolatency"];
    public ObservableCollection<string> ConvertVideoPixelFormatOptions { get; } = new();
    public IReadOnlyList<string> ConvertVideoProfileOptions { get; } = [string.Empty, "baseline", "main", "high", "main10"];
    public IReadOnlyList<string> ConvertVideoLevelOptions { get; } = [string.Empty, "3.1", "4.0", "4.1", "5.0", "5.1"];
    public IReadOnlyList<string> ConvertAudioChannelLayoutOptions { get; } = [string.Empty, "mono", "stereo", "2.1", "5.1", "7.1"];

    public AsyncRelayCommand ResetConvertOptionsCommand { get; private set; } = null!;
    public AsyncRelayCommand ApplyBalancedConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand ApplyEfficientH265ConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand ApplyStreamCopyConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand ApplyReferenceAv1ConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand LoadSelectedConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand SaveCurrentConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand DeleteSelectedConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand ImportConvertPresetsCommand { get; private set; } = null!;
    public AsyncRelayCommand ExportSelectedConvertPresetCommand { get; private set; } = null!;
    public AsyncRelayCommand CreateConvertDraftCommand { get; private set; } = null!;
    public AsyncRelayCommand AddConvertToQueueCommand { get; private set; } = null!;
    public AsyncRelayCommand RefreshConvertQueueOverviewCommand { get; private set; } = null!;
    public AsyncRelayCommand CopyConvertCommandPreviewCommand { get; private set; } = null!;
    public AsyncRelayCommand SyncConvertOutputExtensionCommand { get; private set; } = null!;
    public AsyncRelayCommand RefreshConvertCapabilitiesCommand { get; private set; } = null!;
    public AsyncRelayCommand RefreshConvertInputProbeCommand { get; private set; } = null!;
    public AsyncRelayCommand AddConvertBatchInputsCommand { get; private set; } = null!;
    public AsyncRelayCommand ClearConvertBatchInputsCommand { get; private set; } = null!;
    public AsyncRelayCommand CreateConvertBatchDraftsCommand { get; private set; } = null!;
    public AsyncRelayCommand AddConvertBatchToQueueCommand { get; private set; } = null!;
    public AsyncRelayCommand ApplyConvertSuggestedNameCommand { get; private set; } = null!;

    public bool ConvertShowAdvancedOptions { get => _convertShowAdvancedOptions; set => Set(ref _convertShowAdvancedOptions, value); }
    public string ConvertPresetName { get => _convertPresetName; private set => Set(ref _convertPresetName, value); }
    public string ConvertSelectedPresetName { get => _convertSelectedPresetName; set => Set(ref _convertSelectedPresetName, value); }
    public string ConvertPresetDraftName { get => _convertPresetDraftName; set => Set(ref _convertPresetDraftName, value); }
    public string ConvertPresetLibraryStatus { get => _convertPresetLibraryStatus; private set => Set(ref _convertPresetLibraryStatus, value); }
    public string ConvertAdaptivePresetSummary { get => _convertAdaptivePresetSummary; private set => Set(ref _convertAdaptivePresetSummary, value); }
    public string ConvertOptimizationSummary { get => _convertOptimizationSummary; private set => Set(ref _convertOptimizationSummary, value); }
    public string ConvertQueueSummary { get => _convertQueueSummary; private set => Set(ref _convertQueueSummary, value); }
    public string ConvertQueueSnapshotSummary { get => _convertQueueSnapshotSummary; private set => Set(ref _convertQueueSnapshotSummary, value); }
    public string ConvertLastQueueJobSummary { get => _convertLastQueueJobSummary; private set => Set(ref _convertLastQueueJobSummary, value); }
    public string ConvertQueueHistoryStatus { get => _convertQueueHistoryStatus; private set => Set(ref _convertQueueHistoryStatus, value); }
    public string ConvertCommandPreview { get => _convertCommandPreview; private set => Set(ref _convertCommandPreview, value); }
    public string ConvertValidationSummary { get => _convertValidationSummary; private set => Set(ref _convertValidationSummary, value); }
    public string ConvertAdvisorySummary { get => _convertAdvisorySummary; private set => Set(ref _convertAdvisorySummary, value); }
    public string ConvertCapabilitiesStatus { get => _convertCapabilitiesStatus; private set => Set(ref _convertCapabilitiesStatus, value); }
    public string ConvertHardwareAccelerationSummary { get => _convertHardwareAccelerationSummary; private set => Set(ref _convertHardwareAccelerationSummary, value); }
    public string ConvertInputProbeStatus { get => _convertInputProbeStatus; private set => Set(ref _convertInputProbeStatus, value); }
    public string ConvertInputSourceSummary { get => _convertInputSourceSummary; private set => Set(ref _convertInputSourceSummary, value); }
    public string ConvertInputVideoSummary { get => _convertInputVideoSummary; private set => Set(ref _convertInputVideoSummary, value); }
    public string ConvertInputAudioSummary { get => _convertInputAudioSummary; private set => Set(ref _convertInputAudioSummary, value); }
    public string ConvertInputStreamsSummary { get => _convertInputStreamsSummary; private set => Set(ref _convertInputStreamsSummary, value); }
    public string ConvertInputRecommendationSummary { get => _convertInputRecommendationSummary; private set => Set(ref _convertInputRecommendationSummary, value); }
    public string ConvertOutputNamingTemplate { get => _convertOutputNamingTemplate; set => Set(ref _convertOutputNamingTemplate, value); }
    public string ConvertOutputNamePreview { get => _convertOutputNamePreview; private set => Set(ref _convertOutputNamePreview, value); }
    public string ConvertBatchSummary { get => _convertBatchSummary; private set => Set(ref _convertBatchSummary, value); }
    public bool ConvertIsInspectingInput { get => _convertIsInspectingInput; private set => Set(ref _convertIsInspectingInput, value); }
    public bool ConvertHasInputProbe => _convertInputProbeResult is not null && PathsEqual(_convertInputProbeResult.FilePath, ConvertInputPath);
    public bool ConvertHasRecentQueueItems => ConvertRecentQueueItems.Count > 0;
    public bool ConvertHasBatchInputs => ConvertBatchInputPaths.Count > 0;

    public string ConvertContainer { get => _convertContainer; set => Set(ref _convertContainer, value); }
    public bool ConvertOverwriteExisting { get => _convertOverwriteExisting; set => Set(ref _convertOverwriteExisting, value); }
    public bool ConvertFastStart { get => _convertFastStart; set => Set(ref _convertFastStart, value); }
    public bool ConvertUseHardwareAcceleration { get => _convertUseHardwareAcceleration; set => Set(ref _convertUseHardwareAcceleration, value); }

    public StreamProcessingMode ConvertVideoMode { get => _convertVideoMode; set => Set(ref _convertVideoMode, value); }
    public string ConvertVideoCodec { get => _convertVideoCodec; set => Set(ref _convertVideoCodec, value); }
    public VideoRateControlMode ConvertVideoRateControlMode { get => _convertVideoRateControlMode; set => Set(ref _convertVideoRateControlMode, value); }
    public string ConvertVideoCrfText { get => _convertVideoCrfText; set => Set(ref _convertVideoCrfText, value); }
    public string ConvertVideoBitrate { get => _convertVideoBitrate; set => Set(ref _convertVideoBitrate, value); }
    public string ConvertVideoPreset { get => _convertVideoPreset; set => Set(ref _convertVideoPreset, value); }
    public string ConvertVideoTune { get => _convertVideoTune; set => Set(ref _convertVideoTune, value); }
    public string ConvertVideoPixelFormat { get => _convertVideoPixelFormat; set => Set(ref _convertVideoPixelFormat, value); }
    public FrameRateMode ConvertVideoFrameRateMode { get => _convertVideoFrameRateMode; set => Set(ref _convertVideoFrameRateMode, value); }
    public string ConvertVideoFrameRateText { get => _convertVideoFrameRateText; set => Set(ref _convertVideoFrameRateText, value); }
    public ScaleMode ConvertVideoScaleMode { get => _convertVideoScaleMode; set => Set(ref _convertVideoScaleMode, value); }
    public string ConvertVideoWidthText { get => _convertVideoWidthText; set => Set(ref _convertVideoWidthText, value); }
    public string ConvertVideoHeightText { get => _convertVideoHeightText; set => Set(ref _convertVideoHeightText, value); }
    public string ConvertVideoProfile { get => _convertVideoProfile; set => Set(ref _convertVideoProfile, value); }
    public string ConvertVideoLevel { get => _convertVideoLevel; set => Set(ref _convertVideoLevel, value); }
    public string ConvertVideoGopText { get => _convertVideoGopText; set => Set(ref _convertVideoGopText, value); }
    public VideoPassMode ConvertVideoPassMode { get => _convertVideoPassMode; set => Set(ref _convertVideoPassMode, value); }
    public VideoDeinterlaceMode ConvertVideoDeinterlaceMode { get => _convertVideoDeinterlaceMode; set => Set(ref _convertVideoDeinterlaceMode, value); }
    public string ConvertVideoCropXText { get => _convertVideoCropXText; set => Set(ref _convertVideoCropXText, value); }
    public string ConvertVideoCropYText { get => _convertVideoCropYText; set => Set(ref _convertVideoCropYText, value); }
    public string ConvertVideoCropWidthText { get => _convertVideoCropWidthText; set => Set(ref _convertVideoCropWidthText, value); }
    public string ConvertVideoCropHeightText { get => _convertVideoCropHeightText; set => Set(ref _convertVideoCropHeightText, value); }
    public bool ConvertVideoPadEnabled { get => _convertVideoPadEnabled; set => Set(ref _convertVideoPadEnabled, value); }
    public string ConvertVideoPadWidthText { get => _convertVideoPadWidthText; set => Set(ref _convertVideoPadWidthText, value); }
    public string ConvertVideoPadHeightText { get => _convertVideoPadHeightText; set => Set(ref _convertVideoPadHeightText, value); }
    public string ConvertVideoPadXText { get => _convertVideoPadXText; set => Set(ref _convertVideoPadXText, value); }
    public string ConvertVideoPadYText { get => _convertVideoPadYText; set => Set(ref _convertVideoPadYText, value); }

    public StreamProcessingMode ConvertAudioMode { get => _convertAudioMode; set => Set(ref _convertAudioMode, value); }
    public string ConvertAudioCodec { get => _convertAudioCodec; set => Set(ref _convertAudioCodec, value); }
    public string ConvertAudioBitrate { get => _convertAudioBitrate; set => Set(ref _convertAudioBitrate, value); }
    public string ConvertAudioSampleRateText { get => _convertAudioSampleRateText; set => Set(ref _convertAudioSampleRateText, value); }
    public string ConvertAudioChannelsText { get => _convertAudioChannelsText; set => Set(ref _convertAudioChannelsText, value); }
    public string ConvertAudioChannelLayout { get => _convertAudioChannelLayout; set => Set(ref _convertAudioChannelLayout, value); }
    public string ConvertAudioAdditionalStreamIndexesText { get => _convertAudioAdditionalStreamIndexesText; set => Set(ref _convertAudioAdditionalStreamIndexesText, value); }
    public AudioNormalizationMode ConvertAudioNormalizationMode { get => _convertAudioNormalizationMode; set => Set(ref _convertAudioNormalizationMode, value); }
    public string ConvertAudioLoudnessTargetText { get => _convertAudioLoudnessTargetText; set => Set(ref _convertAudioLoudnessTargetText, value); }
    public string ConvertAudioTruePeakText { get => _convertAudioTruePeakText; set => Set(ref _convertAudioTruePeakText, value); }
    public string ConvertAudioLoudnessRangeText { get => _convertAudioLoudnessRangeText; set => Set(ref _convertAudioLoudnessRangeText, value); }
    public int? ConvertVideoSourceStreamIndex { get => _convertVideoSourceStreamIndex; set => Set(ref _convertVideoSourceStreamIndex, value); }
    public int? ConvertAudioSourceStreamIndex { get => _convertAudioSourceStreamIndex; set => Set(ref _convertAudioSourceStreamIndex, value); }
    public SubtitleProcessingMode ConvertSubtitleMode { get => _convertSubtitleMode; set => Set(ref _convertSubtitleMode, value); }
    public int? ConvertSubtitleSourceStreamIndex { get => _convertSubtitleSourceStreamIndex; set => Set(ref _convertSubtitleSourceStreamIndex, value); }
    public string ConvertSubtitleAdditionalStreamIndexesText { get => _convertSubtitleAdditionalStreamIndexesText; set => Set(ref _convertSubtitleAdditionalStreamIndexesText, value); }
    public string ConvertSubtitleCodec { get => _convertSubtitleCodec; set => Set(ref _convertSubtitleCodec, value); }
    public string ConvertSubtitleLanguage { get => _convertSubtitleLanguage; set => Set(ref _convertSubtitleLanguage, value); }
    public bool ConvertSubtitleSetAsDefault { get => _convertSubtitleSetAsDefault; set => Set(ref _convertSubtitleSetAsDefault, value); }
    public bool ConvertMetadataCopySourceMetadata { get => _convertMetadataCopySourceMetadata; set => Set(ref _convertMetadataCopySourceMetadata, value); }
    public bool ConvertMetadataCopyChapters { get => _convertMetadataCopyChapters; set => Set(ref _convertMetadataCopyChapters, value); }
    public string ConvertMetadataTitle { get => _convertMetadataTitle; set => Set(ref _convertMetadataTitle, value); }
    public string ConvertMetadataArtist { get => _convertMetadataArtist; set => Set(ref _convertMetadataArtist, value); }
    public string ConvertMetadataComment { get => _convertMetadataComment; set => Set(ref _convertMetadataComment, value); }

    public bool ConvertSelectedPresetIsBuiltIn => IsBuiltInConvertPreset(ConvertSelectedPresetName);
    public bool ConvertSelectedPresetIsUser => IsUserConvertPreset(ConvertSelectedPresetName);
    public bool ConvertCanDeleteSelectedPreset => ConvertSelectedPresetIsUser;
    public bool ConvertSelectedPresetExists => TryResolveConvertPreset(ConvertSelectedPresetName, out _, out _);

    public bool ConvertVideoIsEncodingEnabled => ConvertVideoMode == StreamProcessingMode.Encode;
    public bool ConvertVideoShowCrf => ConvertVideoIsEncodingEnabled && ConvertVideoRateControlMode == VideoRateControlMode.ConstantQuality;
    public bool ConvertVideoShowBitrate => ConvertVideoIsEncodingEnabled && ConvertVideoRateControlMode == VideoRateControlMode.Bitrate;
    public bool ConvertVideoShowFrameRate => ConvertVideoIsEncodingEnabled && ConvertVideoFrameRateMode == FrameRateMode.SetOutput;
    public bool ConvertVideoShowScale => ConvertVideoIsEncodingEnabled && ConvertVideoScaleMode == ScaleMode.SetOutput;
    public bool ConvertVideoShowTwoPass => ConvertVideoIsEncodingEnabled && ConvertVideoRateControlMode == VideoRateControlMode.Bitrate;
    public bool ConvertVideoCanUseFilterControls => ConvertVideoIsEncodingEnabled;
    public bool ConvertVideoShowPadOptions => ConvertVideoIsEncodingEnabled && ConvertVideoPadEnabled;
    public bool ConvertAudioIsEncodingEnabled => ConvertAudioMode == StreamProcessingMode.Encode;
    public bool ConvertAudioShowNormalizationControls => ConvertAudioIsEncodingEnabled && ConvertAudioNormalizationMode == AudioNormalizationMode.Loudnorm;
    public bool ConvertSubtitleCanSelectStream => ConvertSubtitleMode != SubtitleProcessingMode.Disable;
    public bool ConvertSubtitleIsCopy => ConvertSubtitleMode == SubtitleProcessingMode.Copy;
    public bool ConvertSubtitleIsEncoding => ConvertSubtitleMode == SubtitleProcessingMode.Encode;
    public bool ConvertSubtitleCanConfigureCodec => ConvertSubtitleMode == SubtitleProcessingMode.Encode;
    public bool ConvertSubtitleIsBurnIn => ConvertSubtitleMode == SubtitleProcessingMode.BurnIn;
    public bool ConvertFastStartSupported => SupportsFastStart(ConvertContainer);
    public bool ConvertHardwareAccelerationAvailable => _convertCapabilitiesSnapshot?.HardwareAccelerationMethods.Count > 0;

    public async Task EnsureConvertCapabilitiesInitializedAsync()
    {
        if (_convertCapabilitiesInitialized)
        {
            return;
        }

        _convertCapabilitiesInitialized = true;
        await LoadConvertCapabilitiesAsync(forceRefresh: false);
    }

    public async Task EnsureConvertInputContextInitializedAsync()
    {
        if (_convertInputContextInitialized)
        {
            return;
        }

        _convertInputContextInitialized = true;
        await RefreshConvertInputProbeCoreAsync(immediate: true);
    }

    private void InitializeConvertEditor()
    {
        SeedConvertCapabilityFallbackOptions();
        InitializeConvertBuiltInPresets();
        LoadPersistedConvertPresets();
        ResetConvertInputProbeState();

        ResetConvertOptionsCommand = new AsyncRelayCommand(ResetConvertOptionsAsync);
        ApplyBalancedConvertPresetCommand = new AsyncRelayCommand(ApplyBalancedConvertPresetAsync);
        ApplyEfficientH265ConvertPresetCommand = new AsyncRelayCommand(ApplyEfficientH265ConvertPresetAsync);
        ApplyStreamCopyConvertPresetCommand = new AsyncRelayCommand(ApplyStreamCopyConvertPresetAsync);
        ApplyReferenceAv1ConvertPresetCommand = new AsyncRelayCommand(ApplyReferenceAv1ConvertPresetAsync);
        LoadSelectedConvertPresetCommand = new AsyncRelayCommand(LoadSelectedConvertPresetAsync, () => ConvertSelectedPresetExists);
        SaveCurrentConvertPresetCommand = new AsyncRelayCommand(SaveCurrentConvertPresetAsync, () => !string.IsNullOrWhiteSpace(ConvertPresetDraftName));
        DeleteSelectedConvertPresetCommand = new AsyncRelayCommand(DeleteSelectedConvertPresetAsync, () => ConvertCanDeleteSelectedPreset);
        ImportConvertPresetsCommand = new AsyncRelayCommand(ImportConvertPresetsAsync);
        ExportSelectedConvertPresetCommand = new AsyncRelayCommand(ExportSelectedConvertPresetAsync);
        CreateConvertDraftCommand = new AsyncRelayCommand(CreateConvertDraftAsync);
        AddConvertToQueueCommand = new AsyncRelayCommand(AddConvertToQueueAsync);
        RefreshConvertQueueOverviewCommand = new AsyncRelayCommand(RefreshConvertQueueOverviewAsync);
        CopyConvertCommandPreviewCommand = new AsyncRelayCommand(CopyConvertCommandPreviewAsync, () => !string.IsNullOrWhiteSpace(ConvertCommandPreview));
        SyncConvertOutputExtensionCommand = new AsyncRelayCommand(SyncConvertOutputExtensionAsync);
        RefreshConvertCapabilitiesCommand = new AsyncRelayCommand(RefreshConvertCapabilitiesAsync);
        RefreshConvertInputProbeCommand = new AsyncRelayCommand(RefreshConvertInputProbeAsync, () => !string.IsNullOrWhiteSpace(ConvertInputPath));
        AddConvertBatchInputsCommand = new AsyncRelayCommand(AddConvertBatchInputsAsync);
        ClearConvertBatchInputsCommand = new AsyncRelayCommand(ClearConvertBatchInputsAsync);
        CreateConvertBatchDraftsCommand = new AsyncRelayCommand(CreateConvertBatchDraftsAsync);
        AddConvertBatchToQueueCommand = new AsyncRelayCommand(AddConvertBatchToQueueAsync);
        ApplyConvertSuggestedNameCommand = new AsyncRelayCommand(ApplyConvertSuggestedNameAsync, () => !string.IsNullOrWhiteSpace(ConvertOutputNamePreview));

        ApplyBalancedConvertPreset();
        RefreshPresetCommandStates();
        RefreshConvertPreview();
        _ = RefreshConvertQueueOverviewAsync();
    }

    private void SeedConvertCapabilityFallbackOptions()
    {
        SyncCollection(ConvertContainerOptions, ContainerCatalog.GetUserSelectableContainers());
        SyncCollection(ConvertVideoCodecOptions, DefaultConvertVideoCodecOptions);
        SyncCollection(ConvertAudioCodecOptions, DefaultConvertAudioCodecOptions);
        SyncCollection(ConvertSubtitleCodecOptions, DefaultConvertSubtitleCodecOptions);
        SyncCollection(ConvertVideoPixelFormatOptions, DefaultConvertPixelFormatOptions);
    }

    private void InitializeConvertBuiltInPresets()
    {
        RebuildAdaptiveConvertBuiltInPresets();
        RebuildConvertPresetLibrary();
    }

    private void LoadPersistedConvertPresets()
    {
        try
        {
            _convertUserPresets.Clear();
            foreach (var preset in _settingsPersistence.LoadConvertPresets())
            {
                if (preset.Options is null)
                {
                    continue;
                }

                var normalized = preset.Normalize();
                if (string.IsNullOrWhiteSpace(normalized.Name) || IsBuiltInConvertPreset(normalized.Name))
                {
                    continue;
                }

                _convertUserPresets[normalized.Name] = normalized;
            }

            RebuildConvertPresetLibrary();
            ConvertPresetLibraryStatus = _convertUserPresets.Count == 0
                ? "Built-in presets loaded. Save a preset to keep your own conversion recipes."
                : $"Loaded {_convertUserPresets.Count} saved user preset(s) from local settings.";
        }
        catch (Exception ex)
        {
            _convertUserPresets.Clear();
            RebuildConvertPresetLibrary();
            UpdateConvertAdaptiveGuidance();
            ConvertPresetLibraryStatus = $"Preset library could not be loaded from settings. {ex.Message}";
        }
    }

    private void PersistConvertPresets()
    {
        var records = _convertUserPresets.Values
            .Select(static preset => preset.Normalize())
            .OrderBy(static preset => preset.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _settingsPersistence.SaveConvertPresets(records);
    }

    private void RebuildConvertPresetLibrary()
    {
        var orderedNames = _convertBuiltInPresets.Keys
            .Concat(_convertUserPresets.Keys.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        SyncCollection(ConvertPresetLibraryNames, orderedNames);
        UpdateConvertAdaptiveGuidance();

        if (string.IsNullOrWhiteSpace(ConvertSelectedPresetName) || !ContainsIgnoreCase(ConvertPresetLibraryNames, ConvertSelectedPresetName))
        {
            ConvertSelectedPresetName = ConvertPresetLibraryNames.FirstOrDefault() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(ConvertPresetDraftName))
        {
            ConvertPresetDraftName = ConvertSelectedPresetName;
        }

        RaisePropertyChanged(nameof(ConvertSelectedPresetIsBuiltIn));
        RaisePropertyChanged(nameof(ConvertSelectedPresetIsUser));
        RaisePropertyChanged(nameof(ConvertCanDeleteSelectedPreset));
        RaisePropertyChanged(nameof(ConvertSelectedPresetExists));
        RefreshPresetCommandStates();
    }

    private void RefreshPresetCommandStates()
    {
        LoadSelectedConvertPresetCommand?.NotifyCanExecuteChanged();
        SaveCurrentConvertPresetCommand?.NotifyCanExecuteChanged();
        DeleteSelectedConvertPresetCommand?.NotifyCanExecuteChanged();
        ExportSelectedConvertPresetCommand?.NotifyCanExecuteChanged();
    }

    private void OnStatePropertyChanged(string? propertyName)
    {
        if (propertyName == nameof(ConvertSelectedPresetName))
        {
            if (!string.IsNullOrWhiteSpace(ConvertSelectedPresetName))
            {
                ConvertPresetDraftName = ConvertSelectedPresetName;
            }

            RaisePropertyChanged(nameof(ConvertSelectedPresetIsBuiltIn));
            RaisePropertyChanged(nameof(ConvertSelectedPresetIsUser));
            RaisePropertyChanged(nameof(ConvertCanDeleteSelectedPreset));
            RaisePropertyChanged(nameof(ConvertSelectedPresetExists));
            RefreshPresetCommandStates();
            return;
        }

        if (propertyName == nameof(ConvertPresetDraftName))
        {
            RefreshPresetCommandStates();
            return;
        }

        if (propertyName is null || !ConvertEditorPropertyNames.Contains(propertyName) || _isApplyingConvertPreset || _isApplyingConvertCapabilities)
        {
            return;
        }

        if (ConvertPresetMutationPropertyNames.Contains(propertyName) && !string.Equals(ConvertPresetName, "Custom", StringComparison.Ordinal))
        {
            ConvertPresetName = "Custom";
        }

        if (propertyName == nameof(ConvertInputPath))
        {
            _ = RefreshConvertInputProbeCoreAsync(immediate: false);
            RefreshConvertInputProbeCommand.NotifyCanExecuteChanged();

            if (string.IsNullOrWhiteSpace(ConvertOutputPath))
            {
                ApplyConvertOutputExtension();
            }
        }

        if (propertyName == nameof(ConvertContainer))
        {
            RebuildAdaptiveConvertBuiltInPresets();
            RefreshConvertSubtitleCodecOptions();

            if (!ConvertFastStartSupported && ConvertFastStart)
            {
                ConvertFastStart = false;
                return;
            }
        }

        if (propertyName == nameof(ConvertSubtitleMode))
        {
            RefreshConvertSubtitleCodecOptions();
        }

        NotifyConvertDerivedProperties();
        UpdateConvertAdaptiveGuidance();
        RefreshConvertPreview();
    }

    private void NotifyConvertDerivedProperties()
    {
        RaisePropertyChanged(nameof(ConvertSelectedPresetIsBuiltIn));
        RaisePropertyChanged(nameof(ConvertSelectedPresetIsUser));
        RaisePropertyChanged(nameof(ConvertCanDeleteSelectedPreset));
        RaisePropertyChanged(nameof(ConvertSelectedPresetExists));
        RaisePropertyChanged(nameof(ConvertVideoIsEncodingEnabled));
        RaisePropertyChanged(nameof(ConvertVideoShowCrf));
        RaisePropertyChanged(nameof(ConvertVideoShowBitrate));
        RaisePropertyChanged(nameof(ConvertVideoShowFrameRate));
        RaisePropertyChanged(nameof(ConvertVideoShowScale));
        RaisePropertyChanged(nameof(ConvertVideoShowTwoPass));
        RaisePropertyChanged(nameof(ConvertVideoCanUseFilterControls));
        RaisePropertyChanged(nameof(ConvertVideoShowPadOptions));
        RaisePropertyChanged(nameof(ConvertAudioIsEncodingEnabled));
        RaisePropertyChanged(nameof(ConvertAudioShowNormalizationControls));
        RaisePropertyChanged(nameof(ConvertSubtitleCanSelectStream));
        RaisePropertyChanged(nameof(ConvertSubtitleIsCopy));
        RaisePropertyChanged(nameof(ConvertSubtitleIsEncoding));
        RaisePropertyChanged(nameof(ConvertSubtitleCanConfigureCodec));
        RaisePropertyChanged(nameof(ConvertSubtitleIsBurnIn));
        RaisePropertyChanged(nameof(ConvertFastStartSupported));
        RaisePropertyChanged(nameof(ConvertHardwareAccelerationAvailable));
        RaisePropertyChanged(nameof(ConvertHasInputProbe));
        RaisePropertyChanged(nameof(ConvertHasRecentQueueItems));
        RaisePropertyChanged(nameof(ConvertHasBatchInputs));
    }

    private Task ResetConvertOptionsAsync()
    {
        ApplyBalancedConvertPreset();
        ConvertStatus = "Balanced convert preset loaded.";
        return Task.CompletedTask;
    }

    private Task ApplyBalancedConvertPresetAsync()
    {
        ApplyBalancedConvertPreset();
        ConvertStatus = "Preset applied: Balanced H.264 MP4.";
        return Task.CompletedTask;
    }

    private Task ApplyEfficientH265ConvertPresetAsync()
    {
        ApplyConvertOptions(CreateEfficientH265ConvertPresetOptions(), "Efficient H.265 MP4");
        ConvertStatus = "Preset applied: Efficient H.265 MP4.";
        return Task.CompletedTask;
    }

    private Task ApplyStreamCopyConvertPresetAsync()
    {
        ApplyConvertOptions(CreateStreamCopyConvertPresetOptions(ConvertContainer), "Stream Copy / Remux");
        ConvertStatus = "Preset applied: Stream Copy / Remux.";
        return Task.CompletedTask;
    }

    private Task ApplyReferenceAv1ConvertPresetAsync()
    {
        ApplyConvertOptions(CreateReferenceAv1ConvertPresetOptions(), "AV1 1440p 10-bit MKV");
        ConvertStatus = "Preset applied: AV1 1440p 10-bit MKV.";
        return Task.CompletedTask;
    }

    private Task LoadSelectedConvertPresetAsync()
    {
        if (!TryResolveConvertPreset(ConvertSelectedPresetName, out var presetOptions, out var isBuiltIn))
        {
            ConvertPresetLibraryStatus = "Select a preset from the library before loading it.";
            return Task.CompletedTask;
        }

        ApplyConvertOptions(presetOptions, ConvertSelectedPresetName);
        ConvertPresetLibraryStatus = isBuiltIn
            ? $"Loaded built-in preset '{ConvertSelectedPresetName}'."
            : $"Loaded saved user preset '{ConvertSelectedPresetName}'.";
        ConvertStatus = $"Preset loaded: {ConvertSelectedPresetName}.";
        return Task.CompletedTask;
    }

    private Task SaveCurrentConvertPresetAsync()
    {
        var presetName = NormalizeOptional(ConvertPresetDraftName);
        if (string.IsNullOrWhiteSpace(presetName))
        {
            ConvertPresetLibraryStatus = "Provide a preset name before saving.";
            return Task.CompletedTask;
        }

        if (IsBuiltInConvertPreset(presetName))
        {
            ConvertPresetLibraryStatus = $"'{presetName}' is reserved for a built-in preset. Choose another name for your user preset.";
            return Task.CompletedTask;
        }

        var parseErrors = new List<string>();
        var convertOptions = BuildConvertOptionsFromEditor(parseErrors).Normalize();
        var validationErrors = BuildConvertPresetValidationErrors(convertOptions, parseErrors);
        if (validationErrors.Count > 0)
        {
            ConvertPresetLibraryStatus = "Preset was not saved because the current convert options are invalid.";
            ConvertValidationSummary = FormatMessages(validationErrors);
            return Task.CompletedTask;
        }

        var wasOverwrite = _convertUserPresets.ContainsKey(presetName);
        _convertUserPresets[presetName] = new ConvertPresetRecord(presetName, convertOptions, DateTimeOffset.UtcNow).Normalize();
        PersistConvertPresets();
        RebuildConvertPresetLibrary();

        ConvertSelectedPresetName = presetName;
        ConvertPresetDraftName = presetName;
        ConvertPresetName = presetName;
        ConvertPresetLibraryStatus = wasOverwrite
            ? $"User preset '{presetName}' was updated in local settings."
            : $"User preset '{presetName}' was saved to local settings.";
        ConvertStatus = wasOverwrite
            ? $"Preset updated: {presetName}."
            : $"Preset saved: {presetName}.";
        RefreshConvertPreview();
        return Task.CompletedTask;
    }

    private Task DeleteSelectedConvertPresetAsync()
    {
        if (!IsUserConvertPreset(ConvertSelectedPresetName))
        {
            ConvertPresetLibraryStatus = "Only user presets can be deleted. Built-in presets are read-only.";
            return Task.CompletedTask;
        }

        var deletedPresetName = ConvertSelectedPresetName;
        _convertUserPresets.Remove(deletedPresetName);
        PersistConvertPresets();
        RebuildConvertPresetLibrary();

        if (string.Equals(ConvertPresetName, deletedPresetName, StringComparison.OrdinalIgnoreCase))
        {
            ConvertPresetName = "Custom";
        }

        ConvertPresetLibraryStatus = $"User preset '{deletedPresetName}' was removed from local settings.";
        ConvertStatus = $"Preset deleted: {deletedPresetName}.";
        RefreshConvertPreview();
        return Task.CompletedTask;
    }

    private Task ImportConvertPresetsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import convert presets from JSON",
            Filter = "JSON files (*.json)|*.json|All files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return Task.CompletedTask;
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName);
            var importedRecords = ParseConvertPresetImportPayload(json);

            var addedCount = 0;
            var updatedCount = 0;
            var skippedReserved = 0;
            var skippedInvalid = 0;
            string? firstImportedName = null;

            foreach (var importedRecord in importedRecords)
            {
                var normalized = importedRecord.Normalize();
                if (string.IsNullOrWhiteSpace(normalized.Name))
                {
                    skippedInvalid++;
                    continue;
                }

                if (IsBuiltInConvertPreset(normalized.Name))
                {
                    skippedReserved++;
                    continue;
                }

                var structuralErrors = BuildPortableConvertPresetValidationErrors(normalized.Options);
                if (structuralErrors.Count > 0)
                {
                    skippedInvalid++;
                    continue;
                }

                var savedRecord = normalized with
                {
                    SavedAtUtc = normalized.SavedAtUtc == default ? DateTimeOffset.UtcNow : normalized.SavedAtUtc,
                    Options = normalized.Options.Normalize()
                };

                if (_convertUserPresets.ContainsKey(savedRecord.Name))
                {
                    updatedCount++;
                }
                else
                {
                    addedCount++;
                }

                firstImportedName ??= savedRecord.Name;
                _convertUserPresets[savedRecord.Name] = savedRecord;
            }

            PersistConvertPresets();
            RebuildConvertPresetLibrary();

            if (!string.IsNullOrWhiteSpace(firstImportedName))
            {
                ConvertSelectedPresetName = firstImportedName;
                ConvertPresetDraftName = firstImportedName;
            }

            ConvertPresetLibraryStatus = BuildConvertPresetImportSummary(addedCount, updatedCount, skippedReserved, skippedInvalid, dialog.FileName);
            ConvertStatus = addedCount + updatedCount > 0
                ? "Convert preset import completed."
                : "No importable convert presets were found in the selected JSON file.";
            RefreshConvertPreview();
        }
        catch (Exception ex)
        {
            ConvertPresetLibraryStatus = $"Preset import failed. {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private Task ExportSelectedConvertPresetAsync()
    {
        try
        {
            var exportRecord = ResolveConvertPresetForExport();
            var defaultFileName = SanitizeFileName($"{exportRecord.Name}.json");

            var dialog = new SaveFileDialog
            {
                Title = "Export convert preset to JSON",
                Filter = "JSON files (*.json)|*.json|All files|*.*",
                FileName = string.IsNullOrWhiteSpace(defaultFileName) ? "convert-preset.json" : defaultFileName,
                AddExtension = true,
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
            {
                return Task.CompletedTask;
            }

            var payload = new ConvertPresetExchangeFile(1, [exportRecord]);
            var json = JsonSerializer.Serialize(payload, ConvertPresetJsonOptions);
            File.WriteAllText(dialog.FileName, json);

            ConvertPresetLibraryStatus = $"Preset '{exportRecord.Name}' exported to JSON.";
            ConvertStatus = $"Preset exported: {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ConvertPresetLibraryStatus = $"Preset export failed. {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private async Task CreateConvertDraftAsync()
    {
        var parseErrors = new List<string>();
        var convertOptions = BuildConvertOptionsFromEditor(parseErrors);
        var validationErrors = BuildConvertValidationErrors(convertOptions, parseErrors);
        if (validationErrors.Count > 0)
        {
            ConvertStatus = "Review convert settings before creating a draft.";
            ConvertValidationSummary = FormatMessages(validationErrors);
            ConvertQueueSummary = "Draft creation blocked until the validation issues are resolved.";
            return;
        }

        try
        {
            var job = CreateConvertQueueJob(convertOptions);
            var draftJob = await _jobQueueService.CreateDraftAsync(job);

            ConvertQueueSummary = $"Draft created • {draftJob.Name}";
            ConvertLastQueueJobSummary = BuildConvertQueueJobSummary(draftJob);
            ConvertStatus = $"Convert draft created: {draftJob.Name}.";
            await RefreshConvertQueueOverviewAsync();
            RefreshConvertPreview();
        }
        catch (Exception ex)
        {
            ConvertQueueSummary = "Draft creation failed.";
            ConvertStatus = $"Queue error: {ex.Message}";
        }
    }

    private async Task AddConvertToQueueAsync()
    {
        var parseErrors = new List<string>();
        var convertOptions = BuildConvertOptionsFromEditor(parseErrors);
        var validationErrors = BuildConvertValidationErrors(convertOptions, parseErrors);
        if (validationErrors.Count > 0)
        {
            ConvertStatus = "Review convert settings before adding the job to the queue.";
            ConvertValidationSummary = FormatMessages(validationErrors);
            ConvertQueueSummary = "Queue submission blocked until the validation issues are resolved.";
            return;
        }

        try
        {
            var job = CreateConvertQueueJob(convertOptions);
            var queuedJob = await _jobQueueService.EnqueueAsync(job);

            ConvertQueueSummary = $"Queued job {queuedJob.Id} • {queuedJob.Name}";
            ConvertLastQueueJobSummary = BuildConvertQueueJobSummary(queuedJob);
            ConvertStatus = $"Convert job added to the queue: {queuedJob.Name}.";
            await RefreshConvertQueueOverviewAsync();
            RefreshConvertPreview();
        }
        catch (Exception ex)
        {
            ConvertQueueSummary = "Queue submission failed.";
            ConvertStatus = $"Queue error: {ex.Message}";
        }
    }


    private Task AddConvertBatchInputsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select batch convert input files",
            CheckFileExists = true,
            Multiselect = true,
            Filter = MediaFileFilter
        };

        if (dialog.ShowDialog() != true)
        {
            return Task.CompletedTask;
        }

        foreach (var fileName in dialog.FileNames.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!ConvertBatchInputPaths.Any(existing => existing.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                ConvertBatchInputPaths.Add(fileName);
            }
        }

        ConvertBatchSummary = $"Batch set ready with {ConvertBatchInputPaths.Count} input file(s). Use Create Drafts or Add Batch to Queue to submit them from Convert.";
        NotifyConvertDerivedProperties();
        RefreshConvertPreview();
        return Task.CompletedTask;
    }

    private Task ClearConvertBatchInputsAsync()
    {
        ConvertBatchInputPaths.Clear();
        ConvertBatchSummary = "Batch convert is idle. Add multiple input files to enqueue a whole set from the Convert tab.";
        NotifyConvertDerivedProperties();
        RefreshConvertPreview();
        return Task.CompletedTask;
    }

    private Task ApplyConvertSuggestedNameAsync()
    {
        var suggestedPath = BuildSuggestedConvertOutputPath();
        if (!string.IsNullOrWhiteSpace(suggestedPath))
        {
            ConvertOutputPath = suggestedPath;
            ConvertStatus = "Suggested output name applied.";
        }

        return Task.CompletedTask;
    }

    private Task CreateConvertBatchDraftsAsync()
        => SubmitConvertBatchAsync(asDraft: true);

    private Task AddConvertBatchToQueueAsync()
        => SubmitConvertBatchAsync(asDraft: false);

    private async Task SubmitConvertBatchAsync(bool asDraft)
    {
        if (ConvertBatchInputPaths.Count == 0)
        {
            ConvertBatchSummary = "Batch submission requires at least one input file.";
            return;
        }

        var parseErrors = new List<string>();
        var convertOptions = BuildConvertOptionsFromEditor(parseErrors);
        var validationErrors = BuildConvertValidationErrors(convertOptions, parseErrors);
        if (validationErrors.Count > 0)
        {
            ConvertValidationSummary = FormatMessages(validationErrors);
            ConvertBatchSummary = "Batch submission blocked until the current Convert configuration is valid.";
            return;
        }

        var submitted = new List<MediaJob>();
        foreach (var inputPath in ConvertBatchInputPaths)
        {
            var outputPath = BuildSuggestedConvertOutputPath(inputPath, ResolveBatchOutputDirectory(), convertOptions);
            var job = CreateConvertQueueJob(convertOptions, inputPath, outputPath);
            var savedJob = asDraft
                ? await _jobQueueService.CreateDraftAsync(job)
                : await _jobQueueService.EnqueueAsync(job);
            submitted.Add(savedJob);
        }

        ConvertBatchSummary = asDraft
            ? $"Created {submitted.Count} convert draft(s) from the current batch list."
            : $"Queued {submitted.Count} convert job(s) from the current batch list.";
        if (submitted.Count > 0)
        {
            ConvertLastQueueJobSummary = BuildConvertQueueJobSummary(submitted[0]);
        }

        await RefreshConvertQueueOverviewAsync();
        RefreshConvertPreview();
    }

    private async Task RefreshConvertQueueOverviewAsync()
    {
        try
        {
            var operationName = OperationCatalog.Get(OperationKind.Convert).DisplayName;
            var jobs = (await _jobQueueService.GetAllAsync())
                .Where(job => string.Equals(job.Operation, operationName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(job => job.CreatedAt)
                .ToArray();

            SyncConvertQueueHistoryItems(jobs.Take(6));

            if (jobs.Length == 0)
            {
                ConvertQueueSnapshotSummary = "No Convert jobs have been stored in the queue yet.";
                ConvertLastQueueJobSummary = "No convert job has been submitted yet from the Convert tab.";
                ConvertQueueHistoryStatus = "Create a draft or enqueue a job to populate recent history here.";
                NotifyConvertDerivedProperties();
                return;
            }

            ConvertQueueSnapshotSummary = BuildConvertQueueSnapshotSummary(jobs);
            ConvertLastQueueJobSummary = BuildConvertQueueJobSummary(jobs[0]);
            ConvertQueueHistoryStatus = $"Showing {ConvertRecentQueueItems.Count} most recent Convert job(s) across drafts, queued jobs and execution history.";
            NotifyConvertDerivedProperties();
        }
        catch (Exception ex)
        {
            SyncConvertQueueHistoryItems(Array.Empty<MediaJob>());
            ConvertQueueSnapshotSummary = "Queue snapshot unavailable.";
            ConvertQueueHistoryStatus = $"Queue history refresh failed. {ex.Message}";
            NotifyConvertDerivedProperties();
        }
    }

    private Task CopyConvertCommandPreviewAsync()
    {
        if (!string.IsNullOrWhiteSpace(ConvertCommandPreview))
        {
            Clipboard.SetText(ConvertCommandPreview);
            ConvertStatus = "FFmpeg command copied to clipboard.";
        }

        return Task.CompletedTask;
    }

    private Task SyncConvertOutputExtensionAsync()
    {
        ApplyConvertOutputExtension();
        ConvertStatus = "Output file synchronized with the selected container.";
        return Task.CompletedTask;
    }

    private async Task RefreshConvertCapabilitiesAsync()
        => await LoadConvertCapabilitiesAsync(forceRefresh: true);

    private async Task RefreshConvertInputProbeAsync()
        => await RefreshConvertInputProbeCoreAsync(immediate: true);

    private void ApplyBalancedConvertPreset()
        => ApplyConvertOptions(ConvertIntelligence.BuildAdaptivePresetCatalog(_convertCapabilitiesSnapshot, GetActiveConvertProbe(), ConvertContainer).BuiltInPresets["Balanced H.264 MP4"], "Balanced H.264 MP4");

    private void ApplyConvertOptions(ConvertOptions options, string presetName)
    {
        _isApplyingConvertPreset = true;
        try
        {
            var normalized = options.Normalize();

            ConvertPresetName = presetName;
            ConvertSelectedPresetName = presetName;
            ConvertPresetDraftName = presetName;
            ConvertContainer = normalized.Container;
            ConvertOverwriteExisting = normalized.OverwriteMode == OverwriteMode.Overwrite;
            ConvertFastStart = normalized.FastStart;
            ConvertUseHardwareAcceleration = normalized.UseHardwareAcceleration && ConvertHardwareAccelerationAvailable;

            ConvertVideoMode = normalized.Video.Mode;
            ConvertVideoCodec = string.IsNullOrWhiteSpace(normalized.Video.Codec) ? "libx264" : normalized.Video.Codec;
            ConvertVideoRateControlMode = normalized.Video.RateControlMode;
            ConvertVideoCrfText = normalized.Video.Crf?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoBitrate = normalized.Video.Bitrate ?? string.Empty;
            ConvertVideoPreset = normalized.Video.Preset ?? string.Empty;
            ConvertVideoTune = normalized.Video.Tune ?? string.Empty;
            ConvertVideoPixelFormat = normalized.Video.PixelFormat ?? string.Empty;
            ConvertVideoFrameRateMode = normalized.Video.FrameRateMode;
            ConvertVideoFrameRateText = normalized.Video.FrameRate?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoScaleMode = normalized.Video.ScaleMode;
            ConvertVideoWidthText = normalized.Video.Width?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoHeightText = normalized.Video.Height?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoProfile = normalized.Video.Profile ?? string.Empty;
            ConvertVideoLevel = normalized.Video.Level ?? string.Empty;
            ConvertVideoGopText = normalized.Video.GopSize?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoSourceStreamIndex = normalized.Video.SourceStreamIndex;
            ConvertVideoPassMode = normalized.Video.PassMode;
            ConvertVideoDeinterlaceMode = normalized.Video.DeinterlaceMode;
            ConvertVideoCropXText = normalized.Video.CropX?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoCropYText = normalized.Video.CropY?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoCropWidthText = normalized.Video.CropWidth?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoCropHeightText = normalized.Video.CropHeight?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoPadEnabled = normalized.Video.PadToSize;
            ConvertVideoPadWidthText = normalized.Video.PadWidth?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoPadHeightText = normalized.Video.PadHeight?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoPadXText = normalized.Video.PadX?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertVideoPadYText = normalized.Video.PadY?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

            ConvertAudioMode = normalized.Audio.Mode;
            ConvertAudioCodec = string.IsNullOrWhiteSpace(normalized.Audio.Codec) ? "aac" : normalized.Audio.Codec;
            ConvertAudioBitrate = normalized.Audio.Bitrate ?? string.Empty;
            ConvertAudioSampleRateText = normalized.Audio.SampleRate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertAudioChannelsText = normalized.Audio.Channels?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            ConvertAudioChannelLayout = normalized.Audio.ChannelLayout ?? string.Empty;
            ConvertAudioSourceStreamIndex = normalized.Audio.SourceStreamIndex;
            ConvertAudioAdditionalStreamIndexesText = BuildAdditionalStreamIndexesText(normalized.Audio.AdditionalSourceStreamIndexes, normalized.Audio.SourceStreamIndex);
            ConvertAudioNormalizationMode = normalized.Audio.NormalizationMode;
            ConvertAudioLoudnessTargetText = normalized.Audio.LoudnessTarget?.ToString("0.###", CultureInfo.InvariantCulture) ?? "-16";
            ConvertAudioTruePeakText = normalized.Audio.TruePeak?.ToString("0.###", CultureInfo.InvariantCulture) ?? "-1.5";
            ConvertAudioLoudnessRangeText = normalized.Audio.LoudnessRange?.ToString("0.###", CultureInfo.InvariantCulture) ?? "11";

            var subtitle = normalized.Subtitle ?? SubtitleOptions.Disabled();
            ConvertSubtitleMode = subtitle.Mode;
            ConvertSubtitleSourceStreamIndex = subtitle.SourceStreamIndex;
            ConvertSubtitleAdditionalStreamIndexesText = BuildAdditionalStreamIndexesText(subtitle.AdditionalSourceStreamIndexes, subtitle.SourceStreamIndex);
            ConvertSubtitleCodec = subtitle.Codec ?? ResolveDefaultSubtitleCodec(ConvertContainer);
            ConvertSubtitleLanguage = subtitle.Language ?? string.Empty;
            ConvertSubtitleSetAsDefault = subtitle.SetAsDefault;

            var metadata = normalized.Metadata ?? MetadataOptions.CreateDefault();
            ConvertMetadataCopySourceMetadata = metadata.CopyMetadata;
            ConvertMetadataCopyChapters = metadata.CopyChapters;
            ConvertMetadataTitle = metadata.Title ?? string.Empty;
            ConvertMetadataArtist = metadata.Artist ?? string.Empty;
            ConvertMetadataComment = metadata.Comment ?? string.Empty;

            EnsureSelectedCapabilityBackedValues();

            if (!string.IsNullOrWhiteSpace(ConvertInputPath))
            {
                ApplyConvertOutputExtension();
            }
        }
        finally
        {
            _isApplyingConvertPreset = false;
        }

        NotifyConvertDerivedProperties();
        RefreshConvertPreview();
    }

    private async Task LoadConvertCapabilitiesAsync(bool forceRefresh)
    {
        try
        {
            ConvertCapabilitiesStatus = forceRefresh
                ? "Refreshing FFmpeg capabilities..."
                : "Loading FFmpeg capabilities...";

            var snapshot = forceRefresh
                ? await _toolchainCapabilitiesService.RefreshAsync()
                : await _toolchainCapabilitiesService.GetSnapshotAsync();

            ApplyConvertCapabilities(snapshot);
            ConvertStatus = forceRefresh
                ? "Convert capabilities refreshed from the current FFmpeg installation."
                : "Convert capabilities loaded from the current FFmpeg installation.";
        }
        catch (Exception ex)
        {
            if (_convertCapabilitiesSnapshot is null)
            {
                SeedConvertCapabilityFallbackOptions();
                ConvertCapabilitiesStatus = $"Capability scan unavailable. Using fallback options. {ex.Message}";
                ConvertHardwareAccelerationSummary = "No hardware acceleration data available.";
            }
            else
            {
                ConvertCapabilitiesStatus = $"Capability refresh failed. Keeping the last detected options. {ex.Message}";
                ConvertHardwareAccelerationSummary = BuildHardwareAccelerationSummary(_convertCapabilitiesSnapshot);
            }

            NotifyConvertDerivedProperties();
            RefreshConvertPreview();
        }
    }

    private void ApplyConvertCapabilities(ToolchainCapabilitiesSnapshot snapshot)
    {
        _isApplyingConvertCapabilities = true;
        try
        {
            _convertCapabilitiesSnapshot = snapshot;

            SyncCollection(ConvertContainerOptions, BuildContainerOptions(snapshot));
            SyncCollection(ConvertVideoCodecOptions, BuildOrderedCapabilityOptions(snapshot.VideoEncoders, DefaultConvertVideoCodecOptions));
            SyncCollection(ConvertAudioCodecOptions, BuildOrderedCapabilityOptions(snapshot.AudioEncoders, DefaultConvertAudioCodecOptions));
            SyncCollection(ConvertSubtitleCodecOptions, BuildSubtitleCodecOptions(snapshot));
            SyncCollection(ConvertVideoPixelFormatOptions, BuildPixelFormatOptions(snapshot.PixelFormats));

            ConvertCapabilitiesStatus = $"Detected {ConvertVideoCodecOptions.Count} video encoders, {ConvertAudioCodecOptions.Count} audio encoders, {ConvertSubtitleCodecOptions.Count} subtitle encoders and {ConvertContainerOptions.Count} output containers from {snapshot.Ffmpeg.ResolvedPath}.";
            ConvertHardwareAccelerationSummary = BuildHardwareAccelerationSummary(snapshot);

            if (!ConvertHardwareAccelerationAvailable)
            {
                ConvertUseHardwareAcceleration = false;
            }

            EnsureSelectedCapabilityBackedValues();
        }
        finally
        {
            _isApplyingConvertCapabilities = false;
        }

        RefreshActiveAdaptivePresetIfNeeded();
        NotifyConvertDerivedProperties();
        UpdateConvertAdaptiveGuidance();
        RefreshConvertPreview();
    }

    private void EnsureSelectedCapabilityBackedValues()
    {
        ConvertContainer = EnsureSelectedContainerOption(ConvertContainer, ConvertContainerOptions, DefaultConvertContainerIds, "mp4");

        if (ConvertVideoIsEncodingEnabled)
        {
            ConvertVideoCodec = EnsureSelectedOption(ConvertVideoCodec, ConvertVideoCodecOptions, DefaultConvertVideoCodecOptions, string.Empty);
        }

        if (ConvertAudioIsEncodingEnabled)
        {
            ConvertAudioCodec = EnsureSelectedOption(ConvertAudioCodec, ConvertAudioCodecOptions, DefaultConvertAudioCodecOptions, string.Empty);
        }

        if (ConvertSubtitleCanConfigureCodec)
        {
            ConvertSubtitleCodec = EnsureSelectedOption(ConvertSubtitleCodec, ConvertSubtitleCodecOptions, DefaultConvertSubtitleCodecOptions, ResolveDefaultSubtitleCodec(ConvertContainer));
        }

        if (!string.IsNullOrWhiteSpace(ConvertVideoPixelFormat) && !ContainsIgnoreCase(ConvertVideoPixelFormatOptions, ConvertVideoPixelFormat))
        {
            ConvertVideoPixelFormat = string.Empty;
        }
    }

    private void RebuildAdaptiveConvertBuiltInPresets()
    {
        var catalog = ConvertIntelligence.BuildAdaptivePresetCatalog(_convertCapabilitiesSnapshot, GetActiveConvertProbe(), ConvertContainer);

        _convertBuiltInPresets.Clear();
        foreach (var entry in catalog.BuiltInPresets)
        {
            _convertBuiltInPresets[entry.Key] = entry.Value;
        }

        ConvertAdaptivePresetSummary = catalog.Summary;
    }

    private void UpdateConvertAdaptiveGuidance()
    {
        if (_convertBuiltInPresets.Count == 0)
        {
            RebuildAdaptiveConvertBuiltInPresets();
        }

        if (string.IsNullOrWhiteSpace(ConvertAdaptivePresetSummary))
        {
            ConvertAdaptivePresetSummary = ConvertIntelligence.BuildAdaptivePresetCatalog(_convertCapabilitiesSnapshot, GetActiveConvertProbe(), ConvertContainer).Summary;
        }
    }

    private void RefreshActiveAdaptivePresetIfNeeded()
    {
        if (_isApplyingConvertPreset || !IsBuiltInConvertPreset(ConvertPresetName))
        {
            return;
        }

        if (TryResolveConvertPreset(ConvertPresetName, out var presetOptions, out _))
        {
            ApplyConvertOptions(presetOptions, ConvertPresetName);
        }
    }

    private ConvertOptions CreateEfficientH265ConvertPresetOptions()
        => ConvertIntelligence.BuildAdaptivePresetCatalog(_convertCapabilitiesSnapshot, GetActiveConvertProbe(), ConvertContainer).BuiltInPresets["Efficient H.265 MP4"];

    private ConvertOptions CreateStreamCopyConvertPresetOptions(string? preferredContainer)
        => ConvertIntelligence.BuildAdaptivePresetCatalog(_convertCapabilitiesSnapshot, GetActiveConvertProbe(), preferredContainer).BuiltInPresets["Stream Copy / Remux"];

    private ConvertOptions CreateReferenceAv1ConvertPresetOptions()
        => ConvertIntelligence.BuildAdaptivePresetCatalog(_convertCapabilitiesSnapshot, GetActiveConvertProbe(), ConvertContainer).BuiltInPresets["AV1 1440p 10-bit MKV"];

    private bool TryResolveConvertPreset(string presetName, out ConvertOptions presetOptions, out bool isBuiltIn)
    {
        if (!string.IsNullOrWhiteSpace(presetName) && _convertBuiltInPresets.TryGetValue(presetName, out var builtInOptions))
        {
            isBuiltIn = true;
            presetOptions = presetName switch
            {
                "Stream Copy / Remux" => CreateStreamCopyConvertPresetOptions(ConvertContainer),
                "AV1 1440p 10-bit MKV" => CreateReferenceAv1ConvertPresetOptions(),
                _ => builtInOptions
            };
            return true;
        }

        if (!string.IsNullOrWhiteSpace(presetName) && _convertUserPresets.TryGetValue(presetName, out var userPreset))
        {
            isBuiltIn = false;
            presetOptions = userPreset.Options;
            return true;
        }

        isBuiltIn = false;
        presetOptions = ConvertOptions.CreateBalancedMp4H264();
        return false;
    }

    private bool IsBuiltInConvertPreset(string? presetName)
        => !string.IsNullOrWhiteSpace(presetName) && _convertBuiltInPresets.ContainsKey(presetName);

    private bool IsUserConvertPreset(string? presetName)
        => !string.IsNullOrWhiteSpace(presetName) && _convertUserPresets.ContainsKey(presetName);

    private MediaJob CreateConvertQueueJob(ConvertOptions convertOptions)
        => CreateConvertQueueJob(convertOptions, ConvertInputPath, ConvertOutputPath);

    private MediaJob CreateConvertQueueJob(ConvertOptions convertOptions, string inputPath, string outputPath)
        => new(
            Id: Guid.NewGuid(),
            Name: BuildConvertQueueJobName(inputPath, outputPath),
            Operation: OperationCatalog.Get(OperationKind.Convert).DisplayName,
            Parameters: BuildConvertOperationParameters(convertOptions, inputPath, outputPath),
            CreatedAt: DateTimeOffset.UtcNow,
            CreatedBy: Environment.UserName,
            State: JobState.Draft,
            OutputPath: outputPath);

    private void SyncConvertQueueHistoryItems(IEnumerable<MediaJob> jobs)
    {
        ConvertRecentQueueItems.Clear();
        foreach (var job in jobs)
        {
            ConvertRecentQueueItems.Add(new ConvertQueueHistoryItem(
                job.Id.ToString("D"),
                job.Name,
                BuildConvertQueueJobSummary(job),
                string.IsNullOrWhiteSpace(job.OutputPath) ? "Output path not defined." : job.OutputPath!));
        }
    }

    private static string BuildConvertQueueSnapshotSummary(IReadOnlyCollection<MediaJob> jobs)
    {
        var grouped = jobs
            .GroupBy(job => job.State)
            .ToDictionary(group => group.Key, group => group.Count());

        static int Count(Dictionary<JobState, int> counts, JobState state)
            => counts.TryGetValue(state, out var value) ? value : 0;

        return $"Convert jobs tracked: {jobs.Count}  •  Draft {Count(grouped, JobState.Draft)}  •  Queued {Count(grouped, JobState.Queued)}  •  Running {Count(grouped, JobState.Running)}  •  Completed {Count(grouped, JobState.Succeeded)}  •  Failed {Count(grouped, JobState.Failed)}  •  Cancelled {Count(grouped, JobState.Cancelled)}";
    }

    private static string BuildConvertQueueJobSummary(MediaJob job)
    {
        var parts = new List<string>
        {
            job.State.ToString(),
            job.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(job.OutputPath))
        {
            parts.Add(Path.GetFileName(job.OutputPath));
        }

        if (job.Progress > 0)
        {
            parts.Add($"{job.Progress:0.#}%");
        }

        if (!string.IsNullOrWhiteSpace(job.Error))
        {
            parts.Add(job.Error!);
        }

        return string.Join("  •  ", parts);
    }

    private string BuildConvertQueueJobName()
        => BuildConvertQueueJobName(ConvertInputPath, ConvertOutputPath);

    private string BuildConvertQueueJobName(string inputPath, string outputPath)
    {
        var sourceName = string.IsNullOrWhiteSpace(inputPath)
            ? "source"
            : Path.GetFileName(inputPath);
        var targetName = string.IsNullOrWhiteSpace(outputPath)
            ? $"output{ContainerCatalog.ResolveDefaultExtension(ConvertContainer)}"
            : Path.GetFileName(outputPath);
        return $"Convert • {sourceName} → {targetName}";
    }

    private ConvertPresetRecord ResolveConvertPresetForExport()
    {
        if (TryResolveConvertPreset(ConvertSelectedPresetName, out var presetOptions, out _))
        {
            return new ConvertPresetRecord(
                ConvertSelectedPresetName,
                presetOptions.Normalize(),
                DateTimeOffset.UtcNow);
        }

        var presetName = NormalizeOptional(ConvertPresetDraftName) ?? NormalizeOptional(ConvertPresetName) ?? "ConvertPreset";
        var parseErrors = new List<string>();
        var convertOptions = BuildConvertOptionsFromEditor(parseErrors).Normalize();
        var validationErrors = BuildPortableConvertPresetValidationErrors(convertOptions);
        validationErrors.AddRange(parseErrors);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException("The current convert settings cannot be exported as a preset until they are valid.");
        }

        return new ConvertPresetRecord(presetName, convertOptions, DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<ConvertPresetRecord> ParseConvertPresetImportPayload(string json)
    {
        var exchange = JsonSerializer.Deserialize<ConvertPresetExchangeFile>(json, ConvertPresetJsonOptions);
        if (exchange?.Presets is { Length: > 0 })
        {
            return exchange.Presets;
        }

        var many = JsonSerializer.Deserialize<ConvertPresetRecord[]>(json, ConvertPresetJsonOptions);
        if (many is { Length: > 0 })
        {
            return many;
        }

        var single = JsonSerializer.Deserialize<ConvertPresetRecord>(json, ConvertPresetJsonOptions);
        if (single is not null)
        {
            return [single];
        }

        throw new InvalidOperationException("The JSON file does not contain a valid convert preset payload.");
    }

    private static string BuildConvertPresetImportSummary(int addedCount, int updatedCount, int skippedReserved, int skippedInvalid, string filePath)
    {
        var parts = new List<string>();
        if (addedCount > 0)
        {
            parts.Add($"{addedCount} imported");
        }

        if (updatedCount > 0)
        {
            parts.Add($"{updatedCount} updated");
        }

        if (skippedReserved > 0)
        {
            parts.Add($"{skippedReserved} skipped because the names are reserved by built-in presets");
        }

        if (skippedInvalid > 0)
        {
            parts.Add($"{skippedInvalid} skipped because the JSON entries were invalid");
        }

        var summary = parts.Count == 0
            ? "No valid user presets were imported."
            : string.Join(", ", parts);

        return $"Preset import from '{Path.GetFileName(filePath)}': {summary}.";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "convert-preset.json" : sanitized;
    }

    private List<string> BuildPortableConvertPresetValidationErrors(ConvertOptions convertOptions)
    {
        var errors = new List<string>();
        errors.AddRange(new ConvertRequest("preset-input", $"preset-output.{NormalizeContainer(convertOptions.Container)}", convertOptions).Validate());

        var normalizedContainer = NormalizeContainer(convertOptions.Container);
        if (AudioOnlyContainers.Contains(normalizedContainer) && convertOptions.Video.Mode != StreamProcessingMode.Disable)
        {
            errors.Add($"Container '.{normalizedContainer}' is audio-only. Disable video or choose a video container.");
        }

        return errors.Distinct(StringComparer.Ordinal).ToList();
    }

    private List<string> BuildConvertPresetValidationErrors(ConvertOptions convertOptions, List<string> parseErrors)
    {
        var errors = new List<string>(parseErrors);
        errors.AddRange(new ConvertRequest("preset-input", $"preset-output.{NormalizeContainer(convertOptions.Container)}", convertOptions).Validate());

        var normalizedContainer = NormalizeContainer(convertOptions.Container);
        if (AudioOnlyContainers.Contains(normalizedContainer) && convertOptions.Video.Mode != StreamProcessingMode.Disable)
        {
            errors.Add($"Container '.{normalizedContainer}' is audio-only. Disable video or choose a video container.");
        }

        if (_convertCapabilitiesSnapshot is not null)
        {
            if (!ContainerCatalog.IsAvailable(normalizedContainer, _convertCapabilitiesSnapshot.Muxers))
            {
                errors.Add($"The detected FFmpeg build does not report container '.{normalizedContainer}' as an available muxer.");
            }

            if (convertOptions.Video.Mode == StreamProcessingMode.Encode && !ContainsIgnoreCase(_convertCapabilitiesSnapshot.VideoEncoders, convertOptions.Video.Codec))
            {
                errors.Add($"Video encoder '{convertOptions.Video.Codec}' is not reported by the detected FFmpeg build.");
            }

            if (convertOptions.Audio.Mode == StreamProcessingMode.Encode && !ContainsIgnoreCase(_convertCapabilitiesSnapshot.AudioEncoders, convertOptions.Audio.Codec))
            {
                errors.Add($"Audio encoder '{convertOptions.Audio.Codec}' is not reported by the detected FFmpeg build.");
            }
        }

        if (normalizedContainer.Equals("webm", StringComparison.OrdinalIgnoreCase))
        {
            if (convertOptions.Video.Mode == StreamProcessingMode.Encode && !IsCompatibleEncoder(convertOptions.Video.Codec, WebmCompatibleVideoEncoders))
            {
                errors.Add("WebM output expects a VP8, VP9 or AV1 video encoder.");
            }

            if (convertOptions.Audio.Mode == StreamProcessingMode.Encode && !IsCompatibleEncoder(convertOptions.Audio.Codec, WebmCompatibleAudioEncoders))
            {
                errors.Add("WebM output expects an Opus or Vorbis audio encoder.");
            }
        }

        if (convertOptions.Subtitle is { Mode: SubtitleProcessingMode.Encode, Codec: not null } subtitleEncoding)
        {
            var resolvedCodec = NormalizeOptional(subtitleEncoding.Codec) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(resolvedCodec) && !IsSubtitleCodecCompatibleWithContainer(resolvedCodec, normalizedContainer))
            {
                errors.Add($"Subtitle codec '{resolvedCodec}' is not a good fit for container '.{normalizedContainer}'.");
            }
        }

        return errors.Distinct(StringComparer.Ordinal).ToList();
    }

    private async Task RunConvertAsync()
    {
        var parseErrors = new List<string>();
        var convertOptions = BuildConvertOptionsFromEditor(parseErrors);
        var validationErrors = BuildConvertValidationErrors(convertOptions, parseErrors);
        if (validationErrors.Count > 0)
        {
            ConvertStatus = "Review convert settings before running.";
            ConvertValidationSummary = FormatMessages(validationErrors);
            return;
        }

        try
        {
            ConvertStatus = "Running convert...";
            var exitCode = await _transcodeViewModel.ExecuteAsync(BuildConvertOperationParameters(convertOptions));
            ConvertStatus = exitCode == 0 ? "Convert completed." : $"Convert failed with exit code {exitCode}.";
            RefreshConvertPreview();
        }
        catch (Exception ex)
        {
            ConvertStatus = $"Convert error: {ex.Message}";
        }
    }

    private void RefreshConvertPreview()
    {
        var parseErrors = new List<string>();
        var convertOptions = BuildConvertOptionsFromEditor(parseErrors);
        var validationErrors = BuildConvertValidationErrors(convertOptions, parseErrors);
        var advisories = BuildConvertAdvisories(convertOptions);
        ConvertOptimizationSummary = ConvertIntelligence.BuildOptimizationSummary(convertOptions, _convertCapabilitiesSnapshot, GetActiveConvertProbe(), ConvertContainer);
        ConvertOutputNamePreview = BuildSuggestedConvertOutputPath(ConvertInputPath, null, convertOptions);
        ConvertBatchSummary = ConvertBatchInputPaths.Count == 0
            ? "Batch convert is idle. Add multiple input files to enqueue a whole set from the Convert tab."
            : $"Batch set ready with {ConvertBatchInputPaths.Count} input file(s). Output names will be generated from template '{ConvertOutputNamingTemplate}'.";

        ConvertValidationSummary = validationErrors.Count == 0
            ? "Ready to convert."
            : FormatMessages(validationErrors);

        ConvertAdvisorySummary = advisories.Count == 0
            ? string.Empty
            : FormatMessages(advisories);

        if (validationErrors.Count == 0)
        {
            try
            {
                ConvertCommandPreview = _commandBuilder.BuildTranscode(BuildConvertOperationParameters(convertOptions));
            }
            catch (Exception ex)
            {
                ConvertCommandPreview = "Preview unavailable.";
                ConvertValidationSummary = FormatMessages(new[] { ex.Message });
            }
        }
        else
        {
            ConvertCommandPreview = "Preview unavailable until the validation issues are resolved.";
        }

        if (CopyConvertCommandPreviewCommand is not null)
        {
            CopyConvertCommandPreviewCommand.NotifyCanExecuteChanged();
        }

        ApplyConvertSuggestedNameCommand?.NotifyCanExecuteChanged();
    }

    private OperationParameters BuildConvertOperationParameters(ConvertOptions convertOptions)
        => BuildConvertOperationParameters(convertOptions, ConvertInputPath, ConvertOutputPath);

    private OperationParameters BuildConvertOperationParameters(ConvertOptions convertOptions, string inputPath, string outputPath)
        => new(
            InputPath: inputPath,
            OutputPath: outputPath,
            Start: null,
            End: null,
            SubtitleOffset: TimeSpan.Zero,
            SpeedFactor: 1.0,
            AdditionalInputs: [],
            Flags: new Dictionary<string, string>(),
            EncodingProfile: null,
            ConvertOptions: convertOptions);

    private ConvertOptions BuildConvertOptionsFromEditor(List<string> parseErrors)
    {
        var videoCrf = ParseNullableInt(ConvertVideoCrfText, "Video CRF", parseErrors);
        var videoFrameRate = ParseNullableDouble(ConvertVideoFrameRateText, "Output FPS", parseErrors);
        var videoWidth = ParseNullableInt(ConvertVideoWidthText, "Video width", parseErrors);
        var videoHeight = ParseNullableInt(ConvertVideoHeightText, "Video height", parseErrors);
        var videoGop = ParseNullableInt(ConvertVideoGopText, "GOP size", parseErrors);
        var cropX = ParseNullableInt(ConvertVideoCropXText, "Crop X", parseErrors);
        var cropY = ParseNullableInt(ConvertVideoCropYText, "Crop Y", parseErrors);
        var cropWidth = ParseNullableInt(ConvertVideoCropWidthText, "Crop width", parseErrors);
        var cropHeight = ParseNullableInt(ConvertVideoCropHeightText, "Crop height", parseErrors);
        var padWidth = ParseNullableInt(ConvertVideoPadWidthText, "Pad width", parseErrors);
        var padHeight = ParseNullableInt(ConvertVideoPadHeightText, "Pad height", parseErrors);
        var padX = ParseNullableInt(ConvertVideoPadXText, "Pad X", parseErrors);
        var padY = ParseNullableInt(ConvertVideoPadYText, "Pad Y", parseErrors);

        var audioSampleRate = ParseNullableInt(ConvertAudioSampleRateText, "Audio sample rate", parseErrors);
        var audioChannels = ParseNullableInt(ConvertAudioChannelsText, "Audio channels", parseErrors);
        var audioLoudnessTarget = ParseNullableDouble(ConvertAudioLoudnessTargetText, "Audio loudness target", parseErrors);
        var audioTruePeak = ParseNullableDouble(ConvertAudioTruePeakText, "Audio true peak", parseErrors);
        var audioLoudnessRange = ParseNullableDouble(ConvertAudioLoudnessRangeText, "Audio loudness range", parseErrors);
        var audioAdditionalStreamIndexes = ParseStreamIndexList(ConvertAudioAdditionalStreamIndexesText, "Additional audio streams", parseErrors, ConvertAudioSourceStreamIndex);
        var subtitleAdditionalStreamIndexes = ParseStreamIndexList(ConvertSubtitleAdditionalStreamIndexesText, "Additional subtitle streams", parseErrors, ConvertSubtitleSourceStreamIndex);

        return new ConvertOptions(
            Container: NormalizeContainer(ConvertContainer),
            OverwriteMode: ConvertOverwriteExisting ? OverwriteMode.Overwrite : OverwriteMode.SkipExisting,
            FastStart: ConvertFastStart && ConvertFastStartSupported,
            UseHardwareAcceleration: ConvertUseHardwareAcceleration,
            Video: new VideoEncodingOptions(
                Mode: ConvertVideoMode,
                Codec: NormalizeOptional(ConvertVideoCodec) ?? string.Empty,
                RateControlMode: ConvertVideoRateControlMode,
                Crf: videoCrf,
                Bitrate: NormalizeOptional(ConvertVideoBitrate),
                Preset: NormalizeOptional(ConvertVideoPreset),
                Tune: NormalizeOptional(ConvertVideoTune),
                PixelFormat: NormalizeOptional(ConvertVideoPixelFormat),
                FrameRateMode: ConvertVideoFrameRateMode,
                FrameRate: videoFrameRate,
                ScaleMode: ConvertVideoScaleMode,
                Width: videoWidth,
                Height: videoHeight,
                Profile: NormalizeOptional(ConvertVideoProfile),
                Level: NormalizeOptional(ConvertVideoLevel),
                GopSize: videoGop,
                SourceStreamIndex: ConvertVideoSourceStreamIndex,
                PassMode: ConvertVideoPassMode,
                DeinterlaceMode: ConvertVideoDeinterlaceMode,
                CropX: cropX,
                CropY: cropY,
                CropWidth: cropWidth,
                CropHeight: cropHeight,
                PadToSize: ConvertVideoPadEnabled,
                PadWidth: padWidth,
                PadHeight: padHeight,
                PadX: padX,
                PadY: padY),
            Audio: new AudioEncodingOptions(
                Mode: ConvertAudioMode,
                Codec: NormalizeOptional(ConvertAudioCodec) ?? string.Empty,
                Bitrate: NormalizeOptional(ConvertAudioBitrate),
                SampleRate: audioSampleRate,
                Channels: audioChannels,
                ChannelLayout: NormalizeOptional(ConvertAudioChannelLayout),
                SourceStreamIndex: ConvertAudioSourceStreamIndex,
                AdditionalSourceStreamIndexes: audioAdditionalStreamIndexes,
                NormalizationMode: ConvertAudioNormalizationMode,
                LoudnessTarget: audioLoudnessTarget,
                TruePeak: audioTruePeak,
                LoudnessRange: audioLoudnessRange),
            Subtitle: new SubtitleOptions(
                Mode: ConvertSubtitleMode,
                SourceStreamIndex: ConvertSubtitleSourceStreamIndex,
                Language: NormalizeOptional(ConvertSubtitleLanguage),
                SetAsDefault: ConvertSubtitleSetAsDefault,
                AdditionalSourceStreamIndexes: subtitleAdditionalStreamIndexes,
                Codec: ConvertSubtitleMode == SubtitleProcessingMode.Encode ? NormalizeOptional(ConvertSubtitleCodec) : null),
            Metadata: new MetadataOptions(
                CopyMetadata: ConvertMetadataCopySourceMetadata,
                CopyChapters: ConvertMetadataCopyChapters,
                Title: NormalizeOptional(ConvertMetadataTitle),
                Artist: NormalizeOptional(ConvertMetadataArtist),
                Comment: NormalizeOptional(ConvertMetadataComment)));
    }

    private List<string> BuildConvertValidationErrors(ConvertOptions convertOptions, List<string> parseErrors)
    {
        var errors = new List<string>(parseErrors);
        errors.AddRange(new ConvertRequest(ConvertInputPath, ConvertOutputPath, convertOptions).Validate());

        var normalizedContainer = NormalizeContainer(convertOptions.Container);
        if (AudioOnlyContainers.Contains(normalizedContainer) && convertOptions.Video.Mode != StreamProcessingMode.Disable)
        {
            errors.Add($"Container '.{normalizedContainer}' is audio-only. Disable video or choose a video container.");
        }

        if (ConvertVideoMode != StreamProcessingMode.Encode && ConvertVideoFrameRateMode == FrameRateMode.SetOutput)
        {
            errors.Add("Output FPS can only be changed when video mode is Encode.");
        }

        if (ConvertVideoMode != StreamProcessingMode.Encode && ConvertVideoScaleMode == ScaleMode.SetOutput)
        {
            errors.Add("Scale can only be changed when video mode is Encode.");
        }

        if (ConvertVideoMode != StreamProcessingMode.Encode && ConvertVideoPassMode == VideoPassMode.TwoPass)
        {
            errors.Add("Two-pass can only be enabled when video mode is Encode.");
        }

        if (ConvertVideoMode != StreamProcessingMode.Encode && ConvertVideoDeinterlaceMode != VideoDeinterlaceMode.Off)
        {
            errors.Add("Deinterlace can only be enabled when video mode is Encode.");
        }

        if (ConvertVideoMode != StreamProcessingMode.Encode && (HasCropOverrides() || ConvertVideoPadEnabled))
        {
            errors.Add("Crop and pad controls can only be used when video mode is Encode.");
        }

        if (ConvertAudioMode != StreamProcessingMode.Encode && ConvertAudioNormalizationMode != AudioNormalizationMode.None)
        {
            errors.Add("Audio normalization requires audio mode Encode.");
        }

        if (ConvertSubtitleMode == SubtitleProcessingMode.BurnIn && ConvertVideoMode != StreamProcessingMode.Encode)
        {
            errors.Add("Burn-in subtitles require video mode Encode.");
        }

        if (ConvertSubtitleMode == SubtitleProcessingMode.BurnIn && !string.IsNullOrWhiteSpace(ConvertSubtitleAdditionalStreamIndexesText))
        {
            errors.Add("Burn-in subtitles only support one selected subtitle stream. Clear additional subtitle stream indexes.");
        }

        var probe = GetActiveConvertProbe();
        if (probe is not null)
        {
            if (convertOptions.Video.CropWidth.HasValue && probe.Width.HasValue && convertOptions.Video.CropWidth.Value > probe.Width.Value)
            {
                errors.Add($"Crop width {convertOptions.Video.CropWidth.Value} exceeds the detected source width {probe.Width.Value}.");
            }

            if (convertOptions.Video.CropHeight.HasValue && probe.Height.HasValue && convertOptions.Video.CropHeight.Value > probe.Height.Value)
            {
                errors.Add($"Crop height {convertOptions.Video.CropHeight.Value} exceeds the detected source height {probe.Height.Value}.");
            }
        }

        if (_convertCapabilitiesSnapshot is not null)
        {
            if (!ContainerCatalog.IsAvailable(normalizedContainer, _convertCapabilitiesSnapshot.Muxers))
            {
                errors.Add($"The detected FFmpeg build does not report container '.{normalizedContainer}' as an available muxer.");
            }

            if (convertOptions.Video.Mode == StreamProcessingMode.Encode && !ContainsIgnoreCase(_convertCapabilitiesSnapshot.VideoEncoders, convertOptions.Video.Codec))
            {
                errors.Add($"Video encoder '{convertOptions.Video.Codec}' is not reported by the detected FFmpeg build.");
            }

            if (convertOptions.Audio.Mode == StreamProcessingMode.Encode && !ContainsIgnoreCase(_convertCapabilitiesSnapshot.AudioEncoders, convertOptions.Audio.Codec))
            {
                errors.Add($"Audio encoder '{convertOptions.Audio.Codec}' is not reported by the detected FFmpeg build.");
            }

            if (convertOptions.Subtitle is { Mode: SubtitleProcessingMode.Encode, Codec: not null } subtitleEncode
                && (_convertCapabilitiesSnapshot.SubtitleEncoders?.Count ?? 0) > 0
                && !ContainsIgnoreCase(_convertCapabilitiesSnapshot.SubtitleEncoders!, subtitleEncode.Codec))
            {
                errors.Add($"Subtitle encoder '{subtitleEncode.Codec}' is not reported by the detected FFmpeg build.");
            }

            if (convertOptions.UseHardwareAcceleration && _convertCapabilitiesSnapshot.HardwareAccelerationMethods.Count == 0)
            {
                errors.Add("Hardware acceleration was requested, but this FFmpeg build reported no hardware acceleration methods.");
            }
        }

        if (probe is not null)
        {
            if (convertOptions.Video.Mode != StreamProcessingMode.Disable && probe.VideoStreamCount == 0)
            {
                errors.Add("The input file does not contain a video stream. Disable video or choose another source.");
            }

            if (convertOptions.Audio.Mode != StreamProcessingMode.Disable && probe.AudioStreamCount == 0)
            {
                errors.Add("The input file does not contain an audio stream. Disable audio or choose another source.");
            }

            if (ConvertSubtitleMode != SubtitleProcessingMode.Disable && probe.SubtitleStreamCount == 0)
            {
                errors.Add("The input file does not contain a subtitle stream. Disable subtitles or choose another source.");
            }

            if (ConvertVideoSourceStreamIndex.HasValue && ConvertVideoSourceStreamIndex.Value >= probe.VideoStreamCount)
            {
                errors.Add($"Selected video stream index {ConvertVideoSourceStreamIndex.Value} exceeds the detected video stream count {probe.VideoStreamCount}.");
            }

            if (ConvertAudioSourceStreamIndex.HasValue && ConvertAudioSourceStreamIndex.Value >= probe.AudioStreamCount)
            {
                errors.Add($"Selected audio stream index {ConvertAudioSourceStreamIndex.Value} exceeds the detected audio stream count {probe.AudioStreamCount}.");
            }

            foreach (var audioIndex in convertOptions.Audio.AdditionalSourceStreamIndexes ?? [])
            {
                if (audioIndex >= probe.AudioStreamCount)
                {
                    errors.Add($"Selected additional audio stream index {audioIndex} exceeds the detected audio stream count {probe.AudioStreamCount}.");
                }
            }

            if (ConvertSubtitleSourceStreamIndex.HasValue && ConvertSubtitleSourceStreamIndex.Value >= probe.SubtitleStreamCount)
            {
                errors.Add($"Selected subtitle stream index {ConvertSubtitleSourceStreamIndex.Value} exceeds the detected subtitle stream count {probe.SubtitleStreamCount}.");
            }

            foreach (var subtitleIndex in convertOptions.Subtitle?.AdditionalSourceStreamIndexes ?? [])
            {
                if (subtitleIndex >= probe.SubtitleStreamCount)
                {
                    errors.Add($"Selected additional subtitle stream index {subtitleIndex} exceeds the detected subtitle stream count {probe.SubtitleStreamCount}.");
                }
            }

            if (normalizedContainer.Equals("webm", StringComparison.OrdinalIgnoreCase))
            {
                if (convertOptions.Video.Mode == StreamProcessingMode.Copy && !IsCompatibleEncoder(probe.VideoCodec ?? string.Empty, WebmCompatibleVideoEncoders))
                {
                    errors.Add($"WebM stream copy is unlikely to work because the detected source video codec '{probe.VideoCodec ?? "unknown"}' is not VP8, VP9 or AV1.");
                }

                if (convertOptions.Audio.Mode == StreamProcessingMode.Copy && !IsCompatibleEncoder(probe.AudioCodec ?? string.Empty, WebmCompatibleAudioEncoders))
                {
                    errors.Add($"WebM stream copy is unlikely to work because the detected source audio codec '{probe.AudioCodec ?? "unknown"}' is not Opus or Vorbis.");
                }
            }
        }

        if (normalizedContainer.Equals("webm", StringComparison.OrdinalIgnoreCase))
        {
            if (convertOptions.Video.Mode == StreamProcessingMode.Encode && !IsCompatibleEncoder(convertOptions.Video.Codec, WebmCompatibleVideoEncoders))
            {
                errors.Add("WebM output expects a VP8, VP9 or AV1 video encoder.");
            }

            if (convertOptions.Audio.Mode == StreamProcessingMode.Encode && !IsCompatibleEncoder(convertOptions.Audio.Codec, WebmCompatibleAudioEncoders))
            {
                errors.Add("WebM output expects an Opus or Vorbis audio encoder.");
            }
        }

        if (convertOptions.Subtitle is { Mode: SubtitleProcessingMode.Encode, Codec: not null } subtitleEncoding)
        {
            var resolvedCodec = NormalizeOptional(subtitleEncoding.Codec) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(resolvedCodec) && !IsSubtitleCodecCompatibleWithContainer(resolvedCodec, normalizedContainer))
            {
                errors.Add($"Subtitle codec '{resolvedCodec}' is not a good fit for container '.{normalizedContainer}'.");
            }
        }

        return errors.Distinct(StringComparer.Ordinal).ToList();
    }

    private List<string> BuildConvertAdvisories(ConvertOptions convertOptions)
    {
        var advisories = new List<string>();
        var normalizedContainer = NormalizeContainer(convertOptions.Container);
        var probe = GetActiveConvertProbe();

        if (!string.IsNullOrWhiteSpace(ConvertOutputPath))
        {
            var outputExtension = Path.GetExtension(ConvertOutputPath).TrimStart('.').ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(outputExtension)
                && !string.IsNullOrWhiteSpace(normalizedContainer)
                && !ContainerCatalog.MatchesExtension(normalizedContainer, outputExtension))
            {
                advisories.Add($"Output extension '.{outputExtension}' does not match the selected container '.{normalizedContainer}'.");
            }
        }

        if (_convertCapabilitiesSnapshot is null)
        {
            advisories.Add("Capability-driven codec lists are using fallback values until FFmpeg capabilities finish loading.");
        }

        if (!string.IsNullOrWhiteSpace(ConvertInputPath) && probe is null)
        {
            advisories.Add("Source-specific validation is limited until ffprobe metadata is available for the selected input.");
        }

        if (convertOptions.Video.Mode == StreamProcessingMode.Copy && convertOptions.Audio.Mode == StreamProcessingMode.Copy)
        {
            advisories.Add("Video and audio are both set to Copy. This will behave as a remux if the target container accepts the source streams.");
        }

        if (convertOptions.Video.SourceStreamIndex.HasValue || convertOptions.Audio.SourceStreamIndex.HasValue || (convertOptions.Audio.AdditionalSourceStreamIndexes?.Count ?? 0) > 0)
        {
            advisories.Add("Explicit source stream mapping is active. Convert will target the selected video/audio stream indexes instead of FFmpeg default stream selection.");
        }

        if ((convertOptions.Audio.AdditionalSourceStreamIndexes?.Count ?? 0) > 0)
        {
            advisories.Add($"Multiple audio streams are selected. FFmpeg will map {1 + convertOptions.Audio.AdditionalSourceStreamIndexes!.Count} audio streams into the output using the current audio settings.");
        }

        if (convertOptions.Subtitle is { Mode: SubtitleProcessingMode.Copy })
        {
            advisories.Add("Subtitle copy is enabled. Container compatibility now also depends on the selected subtitle stream codec.");
        }
        else if (convertOptions.Subtitle is { Mode: SubtitleProcessingMode.Encode } subtitleEncode)
        {
            advisories.Add($"Subtitle encoding is enabled using codec '{subtitleEncode.Codec ?? ResolveDefaultSubtitleCodec(convertOptions.Container)}'. Verify that the target container supports this subtitle codec.");
        }
        else if (convertOptions.Subtitle is { Mode: SubtitleProcessingMode.BurnIn })
        {
            advisories.Add("Subtitle burn-in is enabled. The selected subtitle stream will be rendered into the video image and subtitle streams will not be preserved separately.");
        }

        if ((convertOptions.Subtitle?.AdditionalSourceStreamIndexes?.Count ?? 0) > 0)
        {
            advisories.Add($"Multiple subtitle streams are selected. FFmpeg will map {1 + convertOptions.Subtitle!.AdditionalSourceStreamIndexes!.Count} subtitle streams into the output.");
        }

        if (!string.IsNullOrWhiteSpace(ConvertOutputNamingTemplate))
        {
            advisories.Add($"Output naming template active: {ConvertOutputNamingTemplate}.");
        }

        if (convertOptions.Video.Mode != StreamProcessingMode.Encode && HasVideoEncodeSpecificOverrides())
        {
            advisories.Add("Video encode-specific settings such as CRF, bitrate, preset, FPS, scale, two-pass or filter controls are ignored unless video mode is Encode.");
        }

        if (convertOptions.Audio.Mode != StreamProcessingMode.Encode && HasAudioEncodeSpecificOverrides())
        {
            advisories.Add("Audio encode-specific settings such as bitrate, sample rate, channels, layout or normalization are ignored unless audio mode is Encode.");
        }

        if (convertOptions.Video.Mode == StreamProcessingMode.Encode && convertOptions.Video.PassMode == VideoPassMode.TwoPass)
        {
            advisories.Add("Two-pass encoding is enabled. FFmpeg will run a first analysis pass to a null target and then a second pass that writes the final file.");
        }

        if (convertOptions.Video.DeinterlaceMode == VideoDeinterlaceMode.Yadif)
        {
            advisories.Add("YADIF deinterlacing is enabled before crop/scale/pad. This is useful for interlaced broadcast or capture sources.");
        }

        if (HasCropOverrides())
        {
            advisories.Add("Crop is enabled. The output frame will be cropped before any scale or pad stage.");
        }

        if (convertOptions.Video.PadToSize)
        {
            advisories.Add("Pad is enabled. The output frame will be padded after crop/scale to fit the requested canvas size.");
        }

        if (convertOptions.Audio.NormalizationMode == AudioNormalizationMode.Loudnorm)
        {
            advisories.Add($"Audio loudness normalization is enabled with target {convertOptions.Audio.LoudnessTarget ?? -16:0.###} LUFS.");
        }
        else if (convertOptions.Audio.NormalizationMode == AudioNormalizationMode.Dynaudnorm)
        {
            advisories.Add("Dynamic audio normalization is enabled with FFmpeg dynaudnorm.");
        }

        if (convertOptions.UseHardwareAcceleration)
        {
            advisories.Add(ConvertHardwareAccelerationAvailable
                ? $"Hardware acceleration is enabled with auto detection. Reported methods: {ConvertHardwareAccelerationSummary}."
                : "Hardware acceleration is enabled with auto detection. The active FFmpeg build did not report any hardware acceleration methods.");
        }

        if (ContainersWithLimitedOpusSupport.Contains(normalizedContainer) && convertOptions.Audio.Mode == StreamProcessingMode.Encode && convertOptions.Audio.Codec.Equals("libopus", StringComparison.OrdinalIgnoreCase))
        {
            advisories.Add($"Container '.{normalizedContainer}' with Opus audio can reduce player compatibility. Prefer MKV, WebM or AAC for broad compatibility.");
        }

        if (!MatroskaFriendlyContainers.Contains(normalizedContainer)
            && convertOptions.Video.Mode == StreamProcessingMode.Encode
            && (convertOptions.Video.Codec.Contains("vp9", StringComparison.OrdinalIgnoreCase)
                || convertOptions.Video.Codec.Contains("av1", StringComparison.OrdinalIgnoreCase)
                || convertOptions.Video.Codec.Equals("ffv1", StringComparison.OrdinalIgnoreCase)))
        {
            advisories.Add($"Encoder '{convertOptions.Video.Codec}' is often more interoperable in MKV or WebM than in '.{normalizedContainer}'.");
        }

        if (probe is not null)
        {
            if (probe.VideoStreamCount == 0 && convertOptions.Video.Mode == StreamProcessingMode.Disable)
            {
                advisories.Add("The source is audio-only. Disabling video is the correct choice for this input.");
            }

            if (probe.AudioStreamCount == 0 && convertOptions.Audio.Mode == StreamProcessingMode.Disable)
            {
                advisories.Add("The source is video-only. Disabling audio is the correct choice for this input.");
            }

            if (convertOptions.Video.Mode == StreamProcessingMode.Copy && !string.IsNullOrWhiteSpace(probe.VideoCodec))
            {
                advisories.Add($"Video stream copy will preserve source codec '{probe.VideoCodec}'.");
            }

            if (convertOptions.Audio.Mode == StreamProcessingMode.Copy && !string.IsNullOrWhiteSpace(probe.AudioCodec))
            {
                advisories.Add($"Audio stream copy will preserve source codec '{probe.AudioCodec}'.");
            }
        }

        advisories.AddRange(ConvertIntelligence.BuildAdvancedCompatibilityAdvisories(convertOptions, _convertCapabilitiesSnapshot, probe, ConvertOutputPath));

        return advisories.Distinct(StringComparer.Ordinal).ToList();
    }

    private bool HasVideoEncodeSpecificOverrides()
        => !string.IsNullOrWhiteSpace(ConvertVideoCrfText)
           || !string.IsNullOrWhiteSpace(ConvertVideoBitrate)
           || !string.IsNullOrWhiteSpace(ConvertVideoPreset)
           || !string.IsNullOrWhiteSpace(ConvertVideoTune)
           || !string.IsNullOrWhiteSpace(ConvertVideoPixelFormat)
           || !string.IsNullOrWhiteSpace(ConvertVideoFrameRateText)
           || !string.IsNullOrWhiteSpace(ConvertVideoWidthText)
           || !string.IsNullOrWhiteSpace(ConvertVideoHeightText)
           || !string.IsNullOrWhiteSpace(ConvertVideoProfile)
           || !string.IsNullOrWhiteSpace(ConvertVideoLevel)
           || !string.IsNullOrWhiteSpace(ConvertVideoGopText)
           || ConvertVideoPassMode == VideoPassMode.TwoPass
           || ConvertVideoDeinterlaceMode != VideoDeinterlaceMode.Off
           || HasCropOverrides()
           || ConvertVideoPadEnabled;

    private bool HasCropOverrides()
        => !string.IsNullOrWhiteSpace(ConvertVideoCropXText)
           || !string.IsNullOrWhiteSpace(ConvertVideoCropYText)
           || !string.IsNullOrWhiteSpace(ConvertVideoCropWidthText)
           || !string.IsNullOrWhiteSpace(ConvertVideoCropHeightText);

    private bool HasAudioEncodeSpecificOverrides()
        => !string.IsNullOrWhiteSpace(ConvertAudioBitrate)
           || !string.IsNullOrWhiteSpace(ConvertAudioSampleRateText)
           || !string.IsNullOrWhiteSpace(ConvertAudioChannelsText)
           || !string.IsNullOrWhiteSpace(ConvertAudioChannelLayout)
           || ConvertAudioNormalizationMode == AudioNormalizationMode.Dynaudnorm
           || (ConvertAudioNormalizationMode == AudioNormalizationMode.Loudnorm
               && (!string.IsNullOrWhiteSpace(ConvertAudioLoudnessTargetText)
                   || !string.IsNullOrWhiteSpace(ConvertAudioTruePeakText)
                   || !string.IsNullOrWhiteSpace(ConvertAudioLoudnessRangeText)));

    private async Task RefreshConvertInputProbeCoreAsync(bool immediate)
    {
        var inputPath = ConvertInputPath?.Trim() ?? string.Empty;
        var requestId = ++_convertInputProbeRequestId;

        _convertInputProbeCancellationSource?.Cancel();
        _convertInputProbeCancellationSource?.Dispose();
        _convertInputProbeCancellationSource = null;

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            ResetConvertInputProbeState("Select an input file to inspect it with ffprobe.");
            RefreshConvertPreview();
            return;
        }

        if (!File.Exists(inputPath))
        {
            ResetConvertInputProbeState("Input file not found yet. ffprobe metadata will appear once the path is valid.");
            RefreshConvertPreview();
            return;
        }

        ConvertIsInspectingInput = true;
        ConvertInputProbeStatus = "Inspecting input with ffprobe...";
        ConvertInputSourceSummary = $"Source file: {Path.GetFileName(inputPath)}";
        ConvertInputVideoSummary = "Video: probing...";
        ConvertInputAudioSummary = "Audio: probing...";
        ConvertInputStreamsSummary = "Streams: probing...";
        ConvertInputRecommendationSummary = "Loading source metadata to refine validation and stream copy checks.";
        _convertInputProbeResult = null;
        NotifyConvertDerivedProperties();
        RefreshConvertPreview();

        var cancellationSource = new CancellationTokenSource();
        _convertInputProbeCancellationSource = cancellationSource;

        try
        {
            if (!immediate)
            {
                await Task.Delay(250, cancellationSource.Token);
            }

            var probeResult = await _ffprobeService.ProbeAsync(inputPath, cancellationSource.Token);
            if (cancellationSource.IsCancellationRequested || requestId != _convertInputProbeRequestId || !PathsEqual(inputPath, ConvertInputPath))
            {
                return;
            }

            ApplyConvertInputProbe(probeResult);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (requestId == _convertInputProbeRequestId)
            {
                ResetConvertInputProbeState($"ffprobe inspection failed. {ex.Message}");
                RefreshConvertPreview();
            }
        }
        finally
        {
            if (requestId == _convertInputProbeRequestId)
            {
                ConvertIsInspectingInput = false;
                RefreshConvertInputProbeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private void ApplyConvertInputProbe(MediaProbeResult probeResult)
    {
        _convertInputProbeResult = probeResult;

        ConvertInputProbeStatus = "Input metadata loaded from ffprobe.";
        ConvertInputSourceSummary = BuildConvertInputSourceSummary(probeResult);
        ConvertInputVideoSummary = BuildConvertInputVideoSummary(probeResult);
        ConvertInputAudioSummary = BuildConvertInputAudioSummary(probeResult);
        ConvertInputStreamsSummary = BuildConvertInputStreamsSummary(probeResult);
        ConvertInputRecommendationSummary = BuildConvertInputRecommendationSummary(probeResult);
        RefreshConvertStreamSelectionsFromProbe(probeResult);

        RebuildAdaptiveConvertBuiltInPresets();
        RefreshActiveAdaptivePresetIfNeeded();
        NotifyConvertDerivedProperties();
        UpdateConvertAdaptiveGuidance();
        RefreshConvertPreview();
    }

    private void ResetConvertInputProbeState(string? statusMessage = null)
    {
        _convertInputProbeResult = null;
        ConvertIsInspectingInput = false;
        ConvertInputProbeStatus = statusMessage ?? "Select an input file to inspect it with ffprobe.";
        ConvertInputSourceSummary = "No input metadata loaded.";
        ConvertInputVideoSummary = "Video: —";
        ConvertInputAudioSummary = "Audio: —";
        ConvertInputStreamsSummary = "Streams: —";
        ConvertInputRecommendationSummary = "ffprobe context will enable smarter validation for Copy, audio-only sources and container compatibility.";
        RefreshConvertStreamSelectionsFromProbe(null);
        RebuildAdaptiveConvertBuiltInPresets();
        RefreshActiveAdaptivePresetIfNeeded();
        NotifyConvertDerivedProperties();
        UpdateConvertAdaptiveGuidance();
    }

    private MediaProbeResult? GetActiveConvertProbe()
        => _convertInputProbeResult is not null && PathsEqual(_convertInputProbeResult.FilePath, ConvertInputPath)
            ? _convertInputProbeResult
            : null;

    private void RefreshConvertStreamSelectionsFromProbe(MediaProbeResult? probeResult)
    {
        SyncCollection(ConvertVideoStreamOptions, BuildConvertSourceStreamOptions(probeResult?.StreamInfos, "video", "Auto / first video stream"));
        SyncCollection(ConvertAudioStreamOptions, BuildConvertSourceStreamOptions(probeResult?.StreamInfos, "audio", "Auto / first audio stream"));
        SyncCollection(ConvertSubtitleStreamOptions, BuildConvertSourceStreamOptions(probeResult?.StreamInfos, "subtitle", "Auto / first subtitle stream"));

        if (!ContainsStreamOption(ConvertVideoStreamOptions, ConvertVideoSourceStreamIndex))
        {
            ConvertVideoSourceStreamIndex = null;
        }

        if (!ContainsStreamOption(ConvertAudioStreamOptions, ConvertAudioSourceStreamIndex))
        {
            ConvertAudioSourceStreamIndex = null;
        }

        if (!ContainsStreamOption(ConvertSubtitleStreamOptions, ConvertSubtitleSourceStreamIndex))
        {
            ConvertSubtitleSourceStreamIndex = null;
        }
    }

    private static IReadOnlyList<ConvertSourceStreamOption> BuildConvertSourceStreamOptions(IEnumerable<MediaStreamInfo>? streams, string streamType, string autoLabel)
    {
        var options = new List<ConvertSourceStreamOption>
        {
            new(null, autoLabel)
        };

        if (streams is null)
        {
            return options;
        }

        options.AddRange(streams
            .Where(stream => stream.StreamType.Equals(streamType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(stream => stream.TypeIndex)
            .Select(BuildConvertSourceStreamOption));

        return options;
    }

    private static ConvertSourceStreamOption BuildConvertSourceStreamOption(MediaStreamInfo stream)
    {
        var parts = new List<string>
        {
            $"{stream.StreamType[0]}:{stream.TypeIndex}",
            stream.CodecName ?? "unknown"
        };

        if (stream.Width.HasValue || stream.Height.HasValue)
        {
            parts.Add($"{stream.Width?.ToString(CultureInfo.InvariantCulture) ?? "?"}x{stream.Height?.ToString(CultureInfo.InvariantCulture) ?? "?"}");
        }

        if (stream.FrameRate.HasValue)
        {
            parts.Add($"{stream.FrameRate.Value:0.###} fps");
        }

        if (stream.SampleRate.HasValue)
        {
            parts.Add($"{stream.SampleRate.Value.ToString(CultureInfo.InvariantCulture)} Hz");
        }

        if (stream.Channels.HasValue)
        {
            parts.Add($"{stream.Channels.Value.ToString(CultureInfo.InvariantCulture)} ch");
        }

        if (!string.IsNullOrWhiteSpace(stream.Language))
        {
            parts.Add(stream.Language!);
        }

        if (!string.IsNullOrWhiteSpace(stream.Title))
        {
            parts.Add(stream.Title!);
        }

        if (stream.IsDefault)
        {
            parts.Add("default");
        }

        return new ConvertSourceStreamOption(stream.TypeIndex, string.Join("  •  ", parts));
    }

    private static bool ContainsStreamOption(IEnumerable<ConvertSourceStreamOption> options, int? value)
        => options.Any(option => option.Value == value);

    private static string FormatMessages(IEnumerable<string> messages)
        => string.Join(Environment.NewLine, messages.Select(static message => $"• {message}"));

    private static int? ParseNullableInt(string rawValue, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (int.TryParse(rawValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        errors.Add($"{label} must be a whole number.");
        return null;
    }

    private static double? ParseNullableDouble(string rawValue, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (double.TryParse(rawValue.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        errors.Add($"{label} must be a valid number.");
        return null;
    }


    private void RefreshConvertSubtitleCodecOptions()
    {
        if (_isApplyingConvertCapabilities)
        {
            return;
        }

        var options = BuildSubtitleCodecOptions(_convertCapabilitiesSnapshot);
        SyncCollection(ConvertSubtitleCodecOptions, options);

        if (ConvertSubtitleCanConfigureCodec)
        {
            ConvertSubtitleCodec = EnsureSelectedOption(ConvertSubtitleCodec, ConvertSubtitleCodecOptions, DefaultConvertSubtitleCodecOptions, ResolveDefaultSubtitleCodec(ConvertContainer));
        }
        else if (string.IsNullOrWhiteSpace(ConvertSubtitleCodec))
        {
            ConvertSubtitleCodec = ResolveDefaultSubtitleCodec(ConvertContainer);
        }
    }

    private static IEnumerable<string> BuildSubtitleCodecOptions(ToolchainCapabilitiesSnapshot? snapshot)
    {
        var capabilityBacked = snapshot?.SubtitleEncoders ?? [];
        var preferred = DefaultConvertSubtitleCodecOptions
            .Where(codec => !string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase));
        return BuildOrderedCapabilityOptions(capabilityBacked, preferred, includeEmpty: false)
            .Prepend("copy")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveDefaultSubtitleCodec(string? container)
        => NormalizeContainer(container ?? string.Empty) switch
        {
            "mp4" or "mov" or "m4v" => "mov_text",
            "webm" => "webvtt",
            _ => "srt"
        };

    private static bool IsSubtitleCodecCompatibleWithContainer(string codec, string container)
    {
        var normalizedCodec = codec.Trim().ToLowerInvariant();
        var normalizedContainer = NormalizeContainer(container);
        return normalizedContainer switch
        {
            "mp4" or "mov" or "m4v" => normalizedCodec is "mov_text" or "tx3g",
            "webm" => normalizedCodec is "webvtt",
            "mkv" => normalizedCodec is "srt" or "subrip" or "ass" or "ssa" or "copy" or "webvtt",
            _ => true
        };
    }

    private static IReadOnlyList<int> ParseStreamIndexList(string? rawValue, string label, List<string> errors, int? primaryIndex)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Array.Empty<int>();
        }

        var values = new List<int>();
        foreach (var token in rawValue.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(token, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
            {
                errors.Add($"{label} must be a comma-separated list of non-negative stream indexes.");
                return Array.Empty<int>();
            }

            if (primaryIndex.HasValue && parsed == primaryIndex.Value)
            {
                continue;
            }

            if (!values.Contains(parsed))
            {
                values.Add(parsed);
            }
        }

        return values;
    }

    private static string BuildAdditionalStreamIndexesText(IReadOnlyList<int>? indexes, int? primaryIndex)
    {
        if (indexes is null || indexes.Count == 0)
        {
            return string.Empty;
        }

        var filtered = indexes
            .Where(index => !primaryIndex.HasValue || index != primaryIndex.Value)
            .Distinct()
            .ToArray();
        return filtered.Length == 0
            ? string.Empty
            : string.Join(", ", filtered.Select(static value => value.ToString(CultureInfo.InvariantCulture)));
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static string NormalizeContainer(string value)
        => ContainerCatalog.NormalizeId(value);

    private static bool SupportsFastStart(string? container)
        => ContainerCatalog.ResolveOrDefault(container, defaultId: "mp4").SupportsFastStart;

    private static void SyncCollection<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private static IEnumerable<ContainerDefinition> BuildContainerOptions(ToolchainCapabilitiesSnapshot snapshot)
        => ContainerCatalog.GetAvailableUserSelectableContainers(snapshot.Muxers);

    private static IEnumerable<string> BuildPixelFormatOptions(IReadOnlyCollection<string> pixelFormats)
    {
        if (pixelFormats.Count == 0)
        {
            return DefaultConvertPixelFormatOptions;
        }

        return BuildOrderedCapabilityOptions(pixelFormats, DefaultConvertPixelFormatOptions.Skip(1), includeEmpty: true);
    }

    private static IEnumerable<string> BuildOrderedCapabilityOptions(IEnumerable<string> supportedValues, IEnumerable<string> preferredOrder, bool includeEmpty = false)
    {
        var normalizedSupported = supportedValues
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedSupported.Count == 0)
        {
            return includeEmpty
                ? [string.Empty, .. preferredOrder.Where(static value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase)]
                : preferredOrder.Where(static value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        var ordered = new List<string>();
        if (includeEmpty)
        {
            ordered.Add(string.Empty);
        }

        foreach (var preferred in preferredOrder)
        {
            if (!string.IsNullOrWhiteSpace(preferred) && ContainsIgnoreCase(normalizedSupported, preferred))
            {
                ordered.Add(normalizedSupported.First(value => value.Equals(preferred, StringComparison.OrdinalIgnoreCase)));
            }
        }

        foreach (var remaining in normalizedSupported.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
        {
            if (!ContainsIgnoreCase(ordered, remaining))
            {
                ordered.Add(remaining);
            }
        }

        return ordered;
    }

    private static string EnsureSelectedContainerOption(string currentValue, IEnumerable<ContainerDefinition> availableValues, IEnumerable<string> preferredOrder, string fallback)
    {
        var available = availableValues.ToArray();
        var normalizedCurrent = NormalizeContainer(currentValue);
        var currentMatch = available.FirstOrDefault(value => value.Id.Equals(normalizedCurrent, StringComparison.OrdinalIgnoreCase));
        if (currentMatch is not null)
        {
            return currentMatch.Id;
        }

        foreach (var preferred in preferredOrder)
        {
            var matched = available.FirstOrDefault(value => value.Id.Equals(preferred, StringComparison.OrdinalIgnoreCase));
            if (matched is not null)
            {
                return matched.Id;
            }
        }

        return available.FirstOrDefault()?.Id ?? fallback;
    }

    private static string EnsureSelectedOption(string currentValue, IEnumerable<string> availableValues, IEnumerable<string> preferredOrder, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(currentValue) && ContainsIgnoreCase(availableValues, currentValue))
        {
            return availableValues.First(value => value.Equals(currentValue, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var preferred in preferredOrder)
        {
            var matched = availableValues.FirstOrDefault(value => value.Equals(preferred, StringComparison.OrdinalIgnoreCase));
            if (matched is not null)
            {
                return matched;
            }
        }

        return availableValues.FirstOrDefault() ?? fallback;
    }

    private static bool ContainsIgnoreCase(IEnumerable<string> values, string candidate)
        => values.Any(value => value.Equals(candidate, StringComparison.OrdinalIgnoreCase));

    private static bool IsCompatibleEncoder(string codec, IEnumerable<string> allowedEncoders)
        => !string.IsNullOrWhiteSpace(codec) && allowedEncoders.Any(allowed => codec.Equals(allowed, StringComparison.OrdinalIgnoreCase));

    private static string BuildHardwareAccelerationSummary(ToolchainCapabilitiesSnapshot snapshot)
        => snapshot.HardwareAccelerationMethods.Count == 0
            ? "No hardware acceleration methods detected."
            : string.Join(", ", snapshot.HardwareAccelerationMethods);

    private void ApplyConvertOutputExtension()
    {
        var suggestedPath = BuildSuggestedConvertOutputPath();
        if (!string.IsNullOrWhiteSpace(suggestedPath))
        {
            ConvertOutputPath = suggestedPath;
        }
    }

    private string BuildSuggestedConvertOutputPath()
        => BuildSuggestedConvertOutputPath(ConvertInputPath, null, ResolveActiveEditorOptionsForNaming());

    private string BuildSuggestedConvertOutputPath(string? inputPath, string? preferredDirectory, ConvertOptions? convertOptions)
    {
        var effectiveInputPath = string.IsNullOrWhiteSpace(inputPath) ? ConvertInputPath : inputPath.Trim();
        var normalizedOptions = (convertOptions ?? ResolveActiveEditorOptionsForNaming()).Normalize();
        var normalizedContainer = NormalizeContainer(normalizedOptions.Container);
        var targetExtension = string.IsNullOrWhiteSpace(normalizedContainer)
            ? string.Empty
            : ContainerCatalog.ResolveDefaultExtension(normalizedContainer);

        if (string.IsNullOrWhiteSpace(effectiveInputPath) && string.IsNullOrWhiteSpace(ConvertOutputPath))
        {
            return string.Empty;
        }

        var directory = !string.IsNullOrWhiteSpace(preferredDirectory)
            ? preferredDirectory
            : (!string.IsNullOrWhiteSpace(ConvertOutputPath)
                ? Path.GetDirectoryName(ConvertOutputPath)
                : Path.GetDirectoryName(effectiveInputPath));

        var baseName = BuildSuggestedConvertBaseName(effectiveInputPath, normalizedOptions);
        if (string.IsNullOrWhiteSpace(targetExtension))
        {
            targetExtension = !string.IsNullOrWhiteSpace(ConvertOutputPath)
                ? Path.GetExtension(ConvertOutputPath)
                : Path.GetExtension(effectiveInputPath);
        }

        if (string.IsNullOrWhiteSpace(directory))
        {
            return $"{baseName}{targetExtension}";
        }

        return Path.Combine(directory, $"{baseName}{targetExtension}");
    }

    private ConvertOptions ResolveActiveEditorOptionsForNaming()
    {
        var parseErrors = new List<string>();
        return BuildConvertOptionsFromEditor(parseErrors).Normalize();
    }

    private string ResolveBatchOutputDirectory()
    {
        if (!string.IsNullOrWhiteSpace(ConvertOutputPath))
        {
            var directory = Path.GetDirectoryName(ConvertOutputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return directory;
            }
        }

        return !string.IsNullOrWhiteSpace(ConvertInputPath)
            ? Path.GetDirectoryName(ConvertInputPath) ?? string.Empty
            : string.Empty;
    }

    private string BuildSuggestedConvertBaseName(string? inputPath, ConvertOptions options)
    {
        var sourceName = !string.IsNullOrWhiteSpace(inputPath)
            ? Path.GetFileNameWithoutExtension(inputPath)
            : (!string.IsNullOrWhiteSpace(ConvertOutputPath) ? Path.GetFileNameWithoutExtension(ConvertOutputPath) : "convert_output");
        var safeSourceName = string.IsNullOrWhiteSpace(sourceName) ? "convert_output" : sourceName;
        var videoCodec = options.Video.Mode switch
        {
            StreamProcessingMode.Copy => "copyv",
            StreamProcessingMode.Disable => "novideo",
            _ => NormalizeOptional(options.Video.Codec) ?? "video"
        };
        var audioCodec = options.Audio.Mode switch
        {
            StreamProcessingMode.Copy => "copya",
            StreamProcessingMode.Disable => "noaudio",
            _ => NormalizeOptional(options.Audio.Codec) ?? "audio"
        };
        var heightToken = options.Video.ScaleMode == ScaleMode.SetOutput && options.Video.Height.HasValue
            ? $"{options.Video.Height.Value}p"
            : (GetActiveConvertProbe()?.Height is int sourceHeight ? $"{sourceHeight}p" : string.Empty);
        var presetToken = NormalizeOptional(ConvertPresetName) ?? "custom";
        var template = NormalizeOptional(ConvertOutputNamingTemplate) ?? "{name}_{vcodec}_{container}";
        var resolved = template
            .Replace("{name}", safeSourceName, StringComparison.OrdinalIgnoreCase)
            .Replace("{preset}", presetToken, StringComparison.OrdinalIgnoreCase)
            .Replace("{container}", options.Container, StringComparison.OrdinalIgnoreCase)
            .Replace("{vcodec}", videoCodec, StringComparison.OrdinalIgnoreCase)
            .Replace("{acodec}", audioCodec, StringComparison.OrdinalIgnoreCase)
            .Replace("{height}", heightToken, StringComparison.OrdinalIgnoreCase)
            .Replace("{fps}", options.Video.FrameRateMode == FrameRateMode.SetOutput && options.Video.FrameRate.HasValue ? options.Video.FrameRate.Value.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{tag}", BuildOutputNameTag(options), StringComparison.OrdinalIgnoreCase);

        var collapsed = string.Join("_", resolved.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return Path.GetFileNameWithoutExtension(SanitizeFileName(collapsed));
    }

    private string BuildOutputNameTag(ConvertOptions options)
    {
        var tags = new List<string>();
        if (options.Video.PassMode == VideoPassMode.TwoPass)
        {
            tags.Add("2pass");
        }

        if (options.Video.DeinterlaceMode != VideoDeinterlaceMode.Off)
        {
            tags.Add("deint");
        }

        if (options.Audio.NormalizationMode != AudioNormalizationMode.None)
        {
            tags.Add("norm");
        }

        if (options.Subtitle?.Mode == SubtitleProcessingMode.BurnIn)
        {
            tags.Add("burnin");
        }

        return string.Join('-', tags);
    }

    private string BuildConvertOutputFilter()
    {
        var entries = new List<string>();
        foreach (var container in ConvertContainerOptions.GroupBy(static value => value.Id, StringComparer.OrdinalIgnoreCase).Select(static group => group.First()))
        {
            entries.Add($"{container.DisplayName} (*{container.DefaultExtension})|*{container.DefaultExtension}");
        }

        entries.Add("All files|*.*");
        return string.Join("|", entries);
    }

    private static string BuildConvertInputSourceSummary(MediaProbeResult probeResult)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(probeResult.Container))
        {
            var normalizedContainer = ContainerCatalog.NormalizeSourceContainer(probeResult.Container);
            var displayName = ContainerCatalog.IsKnown(normalizedContainer)
                ? ContainerCatalog.ResolveDisplayName(normalizedContainer)
                : probeResult.Container;
            parts.Add($"Container: {displayName}");
        }

        parts.Add($"Duration: {FormatDuration(probeResult.Duration)}");

        if (probeResult.SizeBytes > 0)
        {
            parts.Add($"Size: {FormatBytes(probeResult.SizeBytes)}");
        }

        if (probeResult.Tags.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title))
        {
            parts.Add($"Title: {title}");
        }

        return string.Join("  •  ", parts);
    }

    private static string BuildConvertInputVideoSummary(MediaProbeResult probeResult)
    {
        if (probeResult.VideoStreamCount == 0)
        {
            return "Video: no video streams detected.";
        }

        var parts = new List<string>
        {
            $"Video: {probeResult.VideoCodec ?? "unknown"}"
        };

        if (probeResult.Width.HasValue || probeResult.Height.HasValue)
        {
            parts.Add($"{probeResult.Width?.ToString(CultureInfo.InvariantCulture) ?? "?"}x{probeResult.Height?.ToString(CultureInfo.InvariantCulture) ?? "?"}");
        }

        if (probeResult.FrameRate.HasValue)
        {
            parts.Add($"{probeResult.FrameRate.Value.ToString("0.###", CultureInfo.InvariantCulture)} fps");
        }

        return string.Join("  •  ", parts);
    }

    private static string BuildConvertInputAudioSummary(MediaProbeResult probeResult)
    {
        if (probeResult.AudioStreamCount == 0)
        {
            return "Audio: no audio streams detected.";
        }

        var parts = new List<string>
        {
            $"Audio: {probeResult.AudioCodec ?? "unknown"}"
        };

        if (probeResult.AudioSampleRate.HasValue)
        {
            parts.Add($"{probeResult.AudioSampleRate.Value.ToString(CultureInfo.InvariantCulture)} Hz");
        }

        if (probeResult.AudioChannels.HasValue)
        {
            parts.Add($"{probeResult.AudioChannels.Value.ToString(CultureInfo.InvariantCulture)} ch");
        }

        if (!string.IsNullOrWhiteSpace(probeResult.AudioChannelLayout))
        {
            parts.Add(probeResult.AudioChannelLayout);
        }

        return string.Join("  •  ", parts);
    }

    private static string BuildConvertInputStreamsSummary(MediaProbeResult probeResult)
    {
        var parts = new List<string>
        {
            $"Streams: {probeResult.VideoStreamCount} video  •  {probeResult.AudioStreamCount} audio  •  {probeResult.SubtitleStreamCount} subtitle"
        };

        var titledStream = probeResult.StreamInfos.FirstOrDefault(static stream => !string.IsNullOrWhiteSpace(stream.Title));
        if (titledStream is not null)
        {
            parts.Add($"Example tagged stream: {titledStream.StreamType}:{titledStream.TypeIndex} '{titledStream.Title}'");
        }

        return string.Join("  •  ", parts);
    }

    private static string BuildConvertInputRecommendationSummary(MediaProbeResult probeResult)
    {
        if (probeResult.VideoStreamCount == 0 && probeResult.AudioStreamCount > 0)
        {
            return "Detected an audio-only source. Disable video and consider an audio container such as M4A, MP3, FLAC, OGG or WAV.";
        }

        if (probeResult.AudioStreamCount == 0 && probeResult.VideoStreamCount > 0)
        {
            return "Detected a video-only source. Disable audio or use stream copy if you only need to rewrap the video stream.";
        }

        if (probeResult.VideoStreamCount > 0 && probeResult.AudioStreamCount > 0)
        {
            if (probeResult.SubtitleStreamCount > 0)
            {
                return "Source metadata is active. Video, audio and subtitle stream selectors are now populated from ffprobe so you can preserve, burn in or drop subtitles intentionally.";
            }

            return "Source metadata is active. Copy mode and container validations now use the detected stream layout and source codecs.";
        }

        return "The source file does not expose standard audio or video streams. Review the ffprobe output and the selected conversion modes.";
    }

    private static string FormatDuration(TimeSpan duration)
        => duration == TimeSpan.Zero
            ? "00:00:00"
            : duration.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var suffixIndex = 0;
        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return $"{value:0.##} {suffixes[suffixIndex]}";
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(Path.GetFullPath(left.Trim()), Path.GetFullPath(right.Trim()), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }


    public sealed record ConvertQueueHistoryItem(string JobId, string Title, string Summary, string Detail);
    public sealed record ConvertSourceStreamOption(int? Value, string Label);

    private sealed record ConvertPresetExchangeFile(int SchemaVersion, ConvertPresetRecord[] Presets);
}
