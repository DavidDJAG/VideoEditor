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
        public string TrimInputPath { get; set; } = @"C:\media\input.mp4";
        public string TrimOutputPath { get; set; } = @"C:\media\trimmed.mp4";
        public TimeSpan TrimStart { get; set; } = TimeSpan.Zero;
        public TimeSpan TrimEnd { get; set; } = TimeSpan.FromSeconds(10);
        public string TrimStatus { get; set; } = "Idle";

        public string ConcatInputA { get; set; } = @"C:\media\part-a.mp4";
        public string ConcatInputB { get; set; } = @"C:\media\part-b.mp4";
        public string ConcatOutputPath { get; set; } = @"C:\media\joined.mp4";
        public string ConcatStatus { get; set; } = "Idle";

        public string ConvertInputPath { get; set; } = @"C:\media\source.mkv";
        public string ConvertOutputPath { get; set; } = @"C:\media\source_convert.mp4";
        public string ConvertStatus { get; set; } = "Ready to convert.";
        public bool ConvertShowAdvancedOptions { get; set; } = true;
        public string ConvertPresetName { get; set; } = "Balanced H.264 MP4";
        public string ConvertSelectedPresetName { get; set; } = "Balanced H.264 MP4";
        public string ConvertPresetDraftName { get; set; } = "Balanced H.264 MP4";
        public string ConvertPresetLibraryStatus { get; set; } = "Loaded 2 saved user presets from local settings.";
        public string ConvertQueueSummary { get; set; } = "Queue integration is ready. The current convert profile can be enqueued for sequential execution.";
        public string ConvertQueueSnapshotSummary { get; set; } = "Convert jobs tracked: 8  •  Draft 2  •  Queued 1  •  Running 1  •  Completed 3  •  Failed 1  •  Cancelled 0";
        public string ConvertLastQueueJobSummary { get; set; } = "Queued  •  2026-03-29 00:42:11  •  source_convert.mp4";
        public string ConvertQueueHistoryStatus { get; set; } = "Showing 3 most recent Convert job(s) across drafts, queued jobs and execution history.";
        public string ConvertCommandPreview { get; set; } = @"-y -i ""C:\media\source.mkv"" -c:v libx264 -b:v 2500k -preset medium -pix_fmt yuv420p -c:a aac -b:a 160k -movflags +faststart -f mp4 ""C:\media\source_convert.mp4""";
        public string ConvertValidationSummary { get; set; } = "Ready to convert.";
        public string ConvertAdvisorySummary { get; set; } = "• Output extension '.mp4' matches the selected container.";
        public string ConvertCapabilitiesStatus { get; set; } = "Detected 3 video encoders, 3 audio encoders and 4 containers from C:\ffmpeg\bin\ffmpeg.exe.";
        public string ConvertHardwareAccelerationSummary { get; set; } = "d3d11va, dxva2";
        public string ConvertInputProbeStatus { get; set; } = "Input metadata loaded from ffprobe.";
        public string ConvertInputSourceSummary { get; set; } = "Container: matroska,webm  •  Duration: 00:02:35.417  •  Size: 148.2 MB";
        public string ConvertInputVideoSummary { get; set; } = "Video: h264  •  1920x1080  •  59.94 fps";
        public string ConvertInputAudioSummary { get; set; } = "Audio: aac  •  48000 Hz  •  2 ch  •  stereo";
        public string ConvertInputStreamsSummary { get; set; } = "Streams: 1 video  •  1 audio  •  1 subtitle";
        public string ConvertInputRecommendationSummary { get; set; } = "Source metadata is active. Copy mode and container validations now use the detected stream layout and source codecs.";
        public bool ConvertIsInspectingInput { get; set; } = false;
        public bool ConvertHasInputProbe { get; set; } = true;
        public bool ConvertHasRecentQueueItems { get; set; } = true;
        public string ConvertContainer { get; set; } = "mp4";
        public bool ConvertOverwriteExisting { get; set; } = true;
        public bool ConvertFastStart { get; set; } = true;
        public bool ConvertUseHardwareAcceleration { get; set; } = false;
        public VideoEditor.Domain.Models.StreamProcessingMode ConvertVideoMode { get; set; } = VideoEditor.Domain.Models.StreamProcessingMode.Encode;
        public string ConvertVideoCodec { get; set; } = "libx264";
        public VideoEditor.Domain.Models.VideoRateControlMode ConvertVideoRateControlMode { get; set; } = VideoEditor.Domain.Models.VideoRateControlMode.Bitrate;
        public string ConvertVideoCrfText { get; set; } = string.Empty;
        public string ConvertVideoBitrate { get; set; } = "2500k";
        public string ConvertVideoPreset { get; set; } = "medium";
        public string ConvertVideoTune { get; set; } = string.Empty;
        public string ConvertVideoPixelFormat { get; set; } = "yuv420p";
        public VideoEditor.Domain.Models.FrameRateMode ConvertVideoFrameRateMode { get; set; } = VideoEditor.Domain.Models.FrameRateMode.KeepSource;
        public string ConvertVideoFrameRateText { get; set; } = string.Empty;
        public VideoEditor.Domain.Models.ScaleMode ConvertVideoScaleMode { get; set; } = VideoEditor.Domain.Models.ScaleMode.KeepSource;
        public string ConvertVideoWidthText { get; set; } = "1280";
        public string ConvertVideoHeightText { get; set; } = "720";
        public string ConvertVideoProfile { get; set; } = "high";
        public string ConvertVideoLevel { get; set; } = "4.1";
        public string ConvertVideoGopText { get; set; } = "60";
        public VideoEditor.Domain.Models.StreamProcessingMode ConvertAudioMode { get; set; } = VideoEditor.Domain.Models.StreamProcessingMode.Encode;
        public string ConvertAudioCodec { get; set; } = "aac";
        public string ConvertAudioBitrate { get; set; } = "160k";
        public string ConvertAudioSampleRateText { get; set; } = "48000";
        public string ConvertAudioChannelsText { get; set; } = "2";
        public string ConvertAudioChannelLayout { get; set; } = "stereo";
        public bool ConvertSelectedPresetIsBuiltIn { get; set; } = true;
        public bool ConvertSelectedPresetIsUser { get; set; } = false;
        public bool ConvertCanDeleteSelectedPreset { get; set; } = false;
        public bool ConvertSelectedPresetExists { get; set; } = true;
        public bool ConvertVideoIsEncodingEnabled { get; set; } = true;
        public bool ConvertVideoShowCrf { get; set; } = false;
        public bool ConvertVideoShowBitrate { get; set; } = true;
        public bool ConvertVideoShowFrameRate { get; set; } = false;
        public bool ConvertVideoShowScale { get; set; } = false;
        public bool ConvertAudioIsEncodingEnabled { get; set; } = true;
        public bool ConvertFastStartSupported { get; set; } = true;
        public bool ConvertHardwareAccelerationAvailable { get; set; } = true;

        public IEnumerable<DesignConvertQueueHistoryItem> ConvertRecentQueueItems { get; } = new[]
        {
            new DesignConvertQueueHistoryItem("1", "Convert • source.mkv → source_convert.mp4", "Queued  •  2026-03-29 00:42:11  •  source_convert.mp4", @"C:\media\source_convert.mp4"),
            new DesignConvertQueueHistoryItem("2", "Convert • source.mkv → mezzanine.mkv", "Draft  •  2026-03-29 00:31:52  •  mezzanine.mkv", @"C:\media\mezzanine.mkv"),
            new DesignConvertQueueHistoryItem("3", "Convert • source.mkv → archive.webm", "Completed  •  2026-03-28 23:58:04  •  archive.webm", @"C:\media\archive.webm")
        };
        public IEnumerable<string> ConvertContainerOptions { get; } = new[] { "mp4", "mkv", "mov", "avi", "webm", "m4a", "mp3", "wav", "flac", "aac" };
        public IEnumerable<string> ConvertPresetLibraryNames { get; } = new[] { "Balanced H.264 MP4", "Efficient H.265 MP4", "Stream Copy / Remux", "Podcast AAC 96k" };
        public IEnumerable<VideoEditor.Domain.Models.StreamProcessingMode> ConvertStreamProcessingModes { get; } = Enum.GetValues<VideoEditor.Domain.Models.StreamProcessingMode>();
        public IEnumerable<VideoEditor.Domain.Models.VideoRateControlMode> ConvertVideoRateControlModes { get; } = Enum.GetValues<VideoEditor.Domain.Models.VideoRateControlMode>();
        public IEnumerable<VideoEditor.Domain.Models.FrameRateMode> ConvertFrameRateModes { get; } = Enum.GetValues<VideoEditor.Domain.Models.FrameRateMode>();
        public IEnumerable<VideoEditor.Domain.Models.ScaleMode> ConvertScaleModes { get; } = Enum.GetValues<VideoEditor.Domain.Models.ScaleMode>();
        public IEnumerable<string> ConvertVideoCodecOptions { get; } = new[] { "libx264", "libx265", "libsvtav1", "h264_nvenc", "hevc_nvenc", "libvpx-vp9" };
        public IEnumerable<string> ConvertAudioCodecOptions { get; } = new[] { "aac", "libopus", "libmp3lame", "flac", "pcm_s16le", "ac3" };
        public IEnumerable<string> ConvertVideoPresetOptions { get; } = new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" };
        public IEnumerable<string> ConvertVideoTuneOptions { get; } = new[] { string.Empty, "film", "animation", "grain", "stillimage", "fastdecode", "zerolatency" };
        public IEnumerable<string> ConvertVideoPixelFormatOptions { get; } = new[] { string.Empty, "yuv420p", "yuv422p", "yuv444p", "nv12", "p010le" };
        public IEnumerable<string> ConvertVideoProfileOptions { get; } = new[] { string.Empty, "baseline", "main", "high", "main10" };
        public IEnumerable<string> ConvertVideoLevelOptions { get; } = new[] { string.Empty, "3.1", "4.0", "4.1", "5.0", "5.1" };
        public IEnumerable<string> ConvertAudioChannelLayoutOptions { get; } = new[] { string.Empty, "mono", "stereo", "2.1", "5.1", "7.1" };

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
        public ICommand ResetConvertOptionsCommand { get; } = new DesignCommand();
        public ICommand ApplyBalancedConvertPresetCommand { get; } = new DesignCommand();
        public ICommand ApplyEfficientH265ConvertPresetCommand { get; } = new DesignCommand();
        public ICommand ApplyStreamCopyConvertPresetCommand { get; } = new DesignCommand();
        public ICommand LoadSelectedConvertPresetCommand { get; } = new DesignCommand();
        public ICommand SaveCurrentConvertPresetCommand { get; } = new DesignCommand();
        public ICommand DeleteSelectedConvertPresetCommand { get; } = new DesignCommand();
        public ICommand ImportConvertPresetsCommand { get; } = new DesignCommand();
        public ICommand ExportSelectedConvertPresetCommand { get; } = new DesignCommand();
        public ICommand CreateConvertDraftCommand { get; } = new DesignCommand();
        public ICommand AddConvertToQueueCommand { get; } = new DesignCommand();
        public ICommand RefreshConvertQueueOverviewCommand { get; } = new DesignCommand();
        public ICommand CopyConvertCommandPreviewCommand { get; } = new DesignCommand();
        public ICommand SyncConvertOutputExtensionCommand { get; } = new DesignCommand();
        public ICommand RefreshConvertCapabilitiesCommand { get; } = new DesignCommand();
        public ICommand RefreshConvertInputProbeCommand { get; } = new DesignCommand();
    }

    public sealed record DesignConvertQueueHistoryItem(string JobId, string Title, string Summary, string Detail);

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
