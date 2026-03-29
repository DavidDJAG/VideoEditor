using VidEditor.Domain.Models;

namespace VidEditor.Application.Services;

public sealed record ConvertPresetSuggestion(string Name, string Summary, ConvertOptions Options);

public sealed record ConvertPresetCatalog(
    IReadOnlyDictionary<string, ConvertOptions> BuiltInPresets,
    IReadOnlyList<ConvertPresetSuggestion> Suggestions,
    string Summary);

public static class ConvertIntelligence
{
    private static readonly string[] BalancedVideoEncoderPreference =
    [
        "libx264",
        "h264_nvenc",
        "h264_qsv",
        "h264_amf",
        "h264_vaapi",
        "libx265",
        "hevc_nvenc",
        "hevc_qsv",
        "hevc_amf",
        "hevc_vaapi",
        "libsvtav1",
        "libaom-av1",
        "av1_nvenc",
        "av1_qsv",
        "av1_amf",
        "av1_vaapi",
        "libvpx-vp9",
        "ffv1",
        "mjpeg"
    ];

    private static readonly string[] EfficientVideoEncoderPreference =
    [
        "libx265",
        "hevc_nvenc",
        "hevc_qsv",
        "hevc_amf",
        "hevc_vaapi",
        "libsvtav1",
        "libaom-av1",
        "av1_nvenc",
        "av1_qsv",
        "av1_amf",
        "av1_vaapi",
        "libx264",
        "h264_nvenc",
        "h264_qsv",
        "h264_amf",
        "h264_vaapi"
    ];

    private static readonly string[] BalancedAudioEncoderPreference = ["aac", "libopus", "libmp3lame", "ac3", "flac", "libvorbis", "alac", "pcm_s16le"];
    private static readonly string[] EfficientAudioEncoderPreference = ["aac", "libopus", "libvorbis", "ac3", "libmp3lame", "flac"];
    private static readonly HashSet<string> FastStartContainers = new(StringComparer.OrdinalIgnoreCase) { "mp4", "mov", "m4v" };
    private static readonly HashSet<string> AudioOnlyContainers = new(StringComparer.OrdinalIgnoreCase) { "aac", "flac", "m4a", "mp3", "ogg", "opus", "wav" };
    private static readonly HashSet<string> Mp4FamilyContainers = new(StringComparer.OrdinalIgnoreCase) { "mp4", "mov", "m4a", "m4v" };
    private static readonly HashSet<string> WebmFamilyContainers = new(StringComparer.OrdinalIgnoreCase) { "webm" };
    private static readonly HashSet<string> MatroskaFamilyContainers = new(StringComparer.OrdinalIgnoreCase) { "mkv", "webm" };
    private static readonly HashSet<string> AviFamilyContainers = new(StringComparer.OrdinalIgnoreCase) { "avi" };
    private static readonly HashSet<string> Mp4PreferredVideoCodecs = new(StringComparer.OrdinalIgnoreCase) { "h264", "libx264", "h264_nvenc", "h264_qsv", "h264_amf", "h264_vaapi", "hevc", "libx265", "hevc_nvenc", "hevc_qsv", "hevc_amf", "hevc_vaapi", "av1", "libaom-av1", "libsvtav1", "av1_nvenc", "av1_qsv", "av1_amf", "av1_vaapi", "mpeg4", "mjpeg" };
    private static readonly HashSet<string> Mp4PreferredAudioCodecs = new(StringComparer.OrdinalIgnoreCase) { "aac", "ac3", "eac3", "alac", "libmp3lame", "mp3", "pcm_s16le", "pcm_s24le" };
    private static readonly HashSet<string> WebmPreferredVideoCodecs = new(StringComparer.OrdinalIgnoreCase) { "libvpx", "libvpx-vp9", "vp8", "vp9", "av1", "libaom-av1", "libsvtav1", "rav1e", "av1_nvenc", "av1_qsv", "av1_amf", "av1_vaapi" };
    private static readonly HashSet<string> WebmPreferredAudioCodecs = new(StringComparer.OrdinalIgnoreCase) { "libopus", "opus", "libvorbis", "vorbis" };
    private static readonly HashSet<string> MovPreferredVideoCodecs = new(StringComparer.OrdinalIgnoreCase) { "h264", "libx264", "h264_nvenc", "h264_qsv", "h264_amf", "h264_vaapi", "hevc", "libx265", "hevc_nvenc", "hevc_qsv", "hevc_amf", "hevc_vaapi", "prores_ks", "prores", "mjpeg" };
    private static readonly HashSet<string> MovPreferredAudioCodecs = new(StringComparer.OrdinalIgnoreCase) { "aac", "alac", "ac3", "pcm_s16le", "pcm_s24le", "mp3" };
    private static readonly HashSet<string> AviPreferredVideoCodecs = new(StringComparer.OrdinalIgnoreCase) { "mpeg4", "mjpeg", "h264", "libx264", "h264_nvenc" };
    private static readonly HashSet<string> AviPreferredAudioCodecs = new(StringComparer.OrdinalIgnoreCase) { "mp3", "pcm_s16le", "pcm_s24le", "ac3", "aac" };

    public static ConvertPresetCatalog BuildAdaptivePresetCatalog(ToolchainCapabilitiesSnapshot? capabilities, MediaProbeResult? probe, string? preferredContainer = null)
    {
        var balanced = BuildBalancedPreset(capabilities);
        var efficient = BuildEfficientPreset(capabilities);
        var remux = BuildStreamCopyPreset(capabilities, probe, preferredContainer);
        var referenceAv1 = BuildReferenceAv1Preset();

        var presets = new Dictionary<string, ConvertOptions>(StringComparer.OrdinalIgnoreCase)
        {
            ["Balanced H.264 MP4"] = balanced,
            ["Efficient H.265 MP4"] = efficient,
            ["Stream Copy / Remux"] = remux,
            ["AV1 1440p 10-bit MKV"] = referenceAv1
        };

        var suggestions = new List<ConvertPresetSuggestion>
        {
            new("Balanced H.264 MP4", SummarizePreset(balanced, "Best compatibility"), balanced),
            new("Efficient H.265 MP4", SummarizePreset(efficient, "Higher compression"), efficient),
            new("Stream Copy / Remux", SummarizePreset(remux, "No re-encode"), remux),
            new("AV1 1440p 10-bit MKV", "Reference SVT-AV1 preset: libsvtav1 preset 6, CRF 28, yuv420p10le, Opus 128k in MKV.", referenceAv1)
        };

        var summary = string.Join(Environment.NewLine, suggestions.Select(static suggestion => $"• {suggestion.Name}: {suggestion.Summary}"));
        return new ConvertPresetCatalog(presets, suggestions, summary);
    }

    public static IReadOnlyList<string> BuildAdvancedCompatibilityAdvisories(ConvertOptions options, ToolchainCapabilitiesSnapshot? capabilities, MediaProbeResult? probe, string? outputPath)
    {
        var advisories = new List<string>();
        var normalized = options.Normalize();
        var container = NormalizeContainer(normalized.Container);
        var effectiveVideoCodec = ResolveEffectiveCodec(normalized.Video.Mode, normalized.Video.Codec, probe?.VideoCodec);
        var effectiveAudioCodec = ResolveEffectiveCodec(normalized.Audio.Mode, normalized.Audio.Codec, probe?.AudioCodec);

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var outputExtension = Path.GetExtension(outputPath).TrimStart('.');
            if (!string.IsNullOrWhiteSpace(outputExtension)
                && !ContainerCatalog.MatchesExtension(container, outputExtension))
            {
                advisories.Add($"The file extension '.{outputExtension}' does not match the selected container '.{container}'.");
            }
        }

        if (probe is not null && normalized.Video.Mode == StreamProcessingMode.Copy && normalized.Audio.Mode == StreamProcessingMode.Copy)
        {
            if (string.Equals(ContainerCatalog.NormalizeSourceContainer(probe.Container), container, StringComparison.OrdinalIgnoreCase))
            {
                advisories.Add($"Source container '{probe.Container}' matches the selected output container. Copy/Copy will behave as a lightweight remux.");
            }
            else
            {
                advisories.Add($"Both streams are in Copy mode. This is a remux from '{probe.Container}' to '{container}', so stream/container compatibility matters more than encoder settings.");
            }
        }

        AppendContainerCodecCompatibilityWarnings(advisories, container, effectiveVideoCodec, effectiveAudioCodec, normalized.Video.Mode, normalized.Audio.Mode);
        AppendCodecSpecificWarnings(advisories, normalized, container, effectiveVideoCodec, effectiveAudioCodec, probe);
        AppendPresetOptimizationHints(advisories, normalized, capabilities, probe);

        return advisories.Distinct(StringComparer.Ordinal).ToArray();
    }

    public static string BuildOptimizationSummary(ConvertOptions currentOptions, ToolchainCapabilitiesSnapshot? capabilities, MediaProbeResult? probe, string? preferredContainer = null)
    {
        var catalog = BuildAdaptivePresetCatalog(capabilities, probe, preferredContainer);
        var normalizedCurrent = currentOptions.Normalize();

        foreach (var suggestion in catalog.Suggestions)
        {
            if (AreEquivalent(normalizedCurrent, suggestion.Options.Normalize()))
            {
                return $"Current convert options already match the adaptive preset '{suggestion.Name}'. {suggestion.Summary}";
            }
        }

        var unsupportedIssues = new List<string>();
        if (capabilities is not null)
        {
            if (normalizedCurrent.Video.Mode == StreamProcessingMode.Encode
                && !ContainsIgnoreCase(capabilities.VideoEncoders, normalizedCurrent.Video.Codec))
            {
                unsupportedIssues.Add($"video encoder '{normalizedCurrent.Video.Codec}' is not reported by this FFmpeg build");
            }

            if (normalizedCurrent.Audio.Mode == StreamProcessingMode.Encode
                && !ContainsIgnoreCase(capabilities.AudioEncoders, normalizedCurrent.Audio.Codec))
            {
                unsupportedIssues.Add($"audio encoder '{normalizedCurrent.Audio.Codec}' is not reported by this FFmpeg build");
            }

            if (!HasMuxer(capabilities, normalizedCurrent.Container))
            {
                unsupportedIssues.Add($"container '.{normalizedCurrent.Container}' is not reported as an available muxer");
            }
        }

        if (unsupportedIssues.Count > 0)
        {
            return $"The current configuration is not ideal for this FFmpeg installation because {string.Join(", ", unsupportedIssues)}. Recommended adaptive presets:{Environment.NewLine}{catalog.Summary}";
        }

        return $"Adaptive presets for the active FFmpeg installation:{Environment.NewLine}{catalog.Summary}";
    }

    private static ConvertOptions BuildBalancedPreset(ToolchainCapabilitiesSnapshot? capabilities)
    {
        var videoEncoder = ChooseEncoder(capabilities?.VideoEncoders, BalancedVideoEncoderPreference, "libx264");
        var prefersMp4 = IsMp4FriendlyVideo(videoEncoder) && HasMuxer(capabilities, "mp4");
        var audioEncoder = prefersMp4
            ? ChooseEncoder(capabilities?.AudioEncoders, ["aac", "ac3", "libmp3lame", "alac", "libopus"], "aac")
            : ChooseEncoder(capabilities?.AudioEncoders, BalancedAudioEncoderPreference, "aac");

        var container = prefersMp4 && IsMp4FriendlyAudio(audioEncoder)
            ? "mp4"
            : ChooseMuxer(capabilities, ["mkv", "mov", "mp4", "avi", "webm"], "mkv");

        if (string.Equals(container, "webm", StringComparison.OrdinalIgnoreCase) && !IsWebmFriendlyAudio(audioEncoder))
        {
            audioEncoder = ChooseEncoder(capabilities?.AudioEncoders, ["libopus", "libvorbis", "aac"], "libopus");
        }

        var useCrf = UsesCrfFriendlyEncoder(videoEncoder);
        int? crf = useCrf
            ? (IsAv1Encoder(videoEncoder)
                ? 30
                : (videoEncoder.Contains("265", StringComparison.OrdinalIgnoreCase) || videoEncoder.Contains("hevc", StringComparison.OrdinalIgnoreCase) ? 24 : 20))
            : null;
        var bitrate = useCrf ? null : "2500k";

        return new ConvertOptions(
            Container: container,
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: FastStartContainers.Contains(container),
            UseHardwareAcceleration: IsHardwareEncoder(videoEncoder),
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: videoEncoder,
                RateControlMode: useCrf ? VideoRateControlMode.ConstantQuality : VideoRateControlMode.Bitrate,
                Crf: crf,
                Bitrate: bitrate,
                Preset: "medium",
                Tune: null,
                PixelFormat: NeedsCompatibilityPixelFormat(videoEncoder, container) ? "yuv420p" : null,
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.KeepSource,
                Width: null,
                Height: null,
                Profile: null,
                Level: null,
                GopSize: null),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: audioEncoder,
                Bitrate: ResolveDefaultAudioBitrate(audioEncoder),
                SampleRate: null,
                Channels: null,
                ChannelLayout: null),
            Subtitle: SubtitleOptions.Disabled(),
            Metadata: MetadataOptions.CreateDefault());
    }

    private static ConvertOptions BuildEfficientPreset(ToolchainCapabilitiesSnapshot? capabilities)
    {
        var videoEncoder = ChooseEncoder(capabilities?.VideoEncoders, EfficientVideoEncoderPreference, "libx265");
        var isAv1 = IsAv1Encoder(videoEncoder);
        var container = isAv1
            ? ChooseMuxer(capabilities, ["mkv", "webm", "mp4"], "mkv")
            : ChooseMuxer(capabilities, ["mp4", "mkv", "mov"], "mp4");
        var audioPreference = isAv1 && string.Equals(container, "webm", StringComparison.OrdinalIgnoreCase)
            ? new[] { "libopus", "libvorbis", "aac" }
            : EfficientAudioEncoderPreference;
        var audioEncoder = ChooseEncoder(capabilities?.AudioEncoders, audioPreference, isAv1 ? "libopus" : "aac");

        var useCrf = UsesCrfFriendlyEncoder(videoEncoder);
        int? crf = useCrf
            ? (isAv1
                ? 30
                : (videoEncoder.Contains("265", StringComparison.OrdinalIgnoreCase) || videoEncoder.Contains("hevc", StringComparison.OrdinalIgnoreCase) ? 24 : 22))
            : null;
        var bitrate = useCrf ? null : (isAv1 ? "1800k" : "2200k");

        return new ConvertOptions(
            Container: container,
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: FastStartContainers.Contains(container),
            UseHardwareAcceleration: IsHardwareEncoder(videoEncoder),
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: videoEncoder,
                RateControlMode: useCrf ? VideoRateControlMode.ConstantQuality : VideoRateControlMode.Bitrate,
                Crf: crf,
                Bitrate: bitrate,
                Preset: isAv1 ? "slow" : "medium",
                Tune: null,
                PixelFormat: NeedsCompatibilityPixelFormat(videoEncoder, container) ? "yuv420p" : null,
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.KeepSource,
                Width: null,
                Height: null,
                Profile: null,
                Level: null,
                GopSize: null),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: audioEncoder,
                Bitrate: ResolveDefaultAudioBitrate(audioEncoder),
                SampleRate: null,
                Channels: null,
                ChannelLayout: null),
            Subtitle: SubtitleOptions.Disabled(),
            Metadata: MetadataOptions.CreateDefault());
    }

    private static ConvertOptions BuildStreamCopyPreset(ToolchainCapabilitiesSnapshot? capabilities, MediaProbeResult? probe, string? preferredContainer)
    {
        var sourceContainer = ContainerCatalog.NormalizeSourceContainer(probe?.Container);
        var container = !string.IsNullOrWhiteSpace(sourceContainer) && HasMuxer(capabilities, sourceContainer)
            ? sourceContainer
            : ChooseMuxer(capabilities, [NormalizeContainer(preferredContainer), "mkv", "mp4", "mov"], string.IsNullOrWhiteSpace(sourceContainer) ? "mkv" : sourceContainer);

        var hasVideo = probe?.VideoStreamCount is null or > 0;
        var hasAudio = probe?.AudioStreamCount is null or > 0;

        if (probe is not null && probe.VideoStreamCount == 0 && probe.AudioStreamCount > 0 && AudioOnlyContainers.Contains(container))
        {
            hasVideo = false;
        }

        if (!hasVideo && !hasAudio)
        {
            hasVideo = true;
        }

        return new ConvertOptions(
            Container: string.IsNullOrWhiteSpace(container) ? "mkv" : container,
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: FastStartContainers.Contains(container),
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: hasVideo ? StreamProcessingMode.Copy : StreamProcessingMode.Disable,
                Codec: hasVideo ? "copy" : string.Empty,
                RateControlMode: VideoRateControlMode.Bitrate,
                Crf: null,
                Bitrate: null,
                Preset: null,
                Tune: null,
                PixelFormat: null,
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.KeepSource,
                Width: null,
                Height: null,
                Profile: null,
                Level: null,
                GopSize: null),
            Audio: new AudioEncodingOptions(
                Mode: hasAudio ? StreamProcessingMode.Copy : StreamProcessingMode.Disable,
                Codec: hasAudio ? "copy" : string.Empty,
                Bitrate: null,
                SampleRate: null,
                Channels: null,
                ChannelLayout: null),
            Subtitle: probe?.SubtitleStreamCount > 0 ? new SubtitleOptions(SubtitleProcessingMode.Copy, 0) : SubtitleOptions.Disabled(),
            Metadata: MetadataOptions.CreateDefault());
    }

    private static ConvertOptions BuildReferenceAv1Preset()
    {
        return new ConvertOptions(
            Container: "mkv",
            OverwriteMode: OverwriteMode.Overwrite,
            FastStart: false,
            UseHardwareAcceleration: false,
            Video: new VideoEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libsvtav1",
                RateControlMode: VideoRateControlMode.ConstantQuality,
                Crf: 28,
                Bitrate: null,
                Preset: "6",
                Tune: null,
                PixelFormat: "yuv420p10le",
                FrameRateMode: FrameRateMode.KeepSource,
                FrameRate: null,
                ScaleMode: ScaleMode.KeepSource,
                Width: null,
                Height: null,
                Profile: null,
                Level: null,
                GopSize: null),
            Audio: new AudioEncodingOptions(
                Mode: StreamProcessingMode.Encode,
                Codec: "libopus",
                Bitrate: "128k",
                SampleRate: null,
                Channels: null,
                ChannelLayout: null),
            Subtitle: SubtitleOptions.Disabled(),
            Metadata: MetadataOptions.CreateDefault());
    }

    private static void AppendContainerCodecCompatibilityWarnings(List<string> advisories, string container, string? videoCodec, string? audioCodec, StreamProcessingMode videoMode, StreamProcessingMode audioMode)
    {
        if (Mp4FamilyContainers.Contains(container))
        {
            if (!string.IsNullOrWhiteSpace(videoCodec) && !IsCodecAllowed(videoCodec, container, streamType: 'V'))
            {
                advisories.Add($"Container '.{container}' is not an ideal target for video codec '{videoCodec}'. Prefer H.264, HEVC or AV1 in MP4/MOV family outputs.");
            }

            if (!string.IsNullOrWhiteSpace(audioCodec) && !IsCodecAllowed(audioCodec, container, streamType: 'A'))
            {
                advisories.Add($"Container '.{container}' is not an ideal target for audio codec '{audioCodec}'. AAC, AC-3, ALAC or MP3 are safer choices here.");
            }
        }
        else if (WebmFamilyContainers.Contains(container))
        {
            if (videoMode != StreamProcessingMode.Disable && !string.IsNullOrWhiteSpace(videoCodec) && !IsCodecAllowed(videoCodec, container, streamType: 'V'))
            {
                advisories.Add("WebM is best paired with VP8, VP9 or AV1 video.");
            }

            if (audioMode != StreamProcessingMode.Disable && !string.IsNullOrWhiteSpace(audioCodec) && !IsCodecAllowed(audioCodec, container, streamType: 'A'))
            {
                advisories.Add("WebM is best paired with Opus or Vorbis audio.");
            }
        }
        else if (AviFamilyContainers.Contains(container))
        {
            if (!string.IsNullOrWhiteSpace(videoCodec) && !IsCodecAllowed(videoCodec, container, streamType: 'V'))
            {
                advisories.Add("AVI is a weak fit for modern codecs such as HEVC, AV1 or VP9. MKV or MP4 are safer containers.");
            }

            if (!string.IsNullOrWhiteSpace(audioCodec) && !IsCodecAllowed(audioCodec, container, streamType: 'A'))
            {
                advisories.Add("AVI works best with PCM, MP3 or AC-3 audio. Other codecs may reduce interoperability.");
            }
        }
        else if (AudioOnlyContainers.Contains(container) && videoMode != StreamProcessingMode.Disable)
        {
            advisories.Add($"Container '.{container}' is audio-only. Disable video or change the output container.");
        }
    }

    private static void AppendCodecSpecificWarnings(List<string> advisories, ConvertOptions options, string container, string? videoCodec, string? audioCodec, MediaProbeResult? probe)
    {
        if (options.Video.Mode == StreamProcessingMode.Encode
            && NeedsCompatibilityPixelFormat(videoCodec, container)
            && !string.IsNullOrWhiteSpace(options.Video.PixelFormat)
            && !options.Video.PixelFormat.Equals("yuv420p", StringComparison.OrdinalIgnoreCase))
        {
            advisories.Add($"Pixel format '{options.Video.PixelFormat}' can reduce playback compatibility for '{videoCodec}' in '.{container}'. Use yuv420p for the broadest compatibility.");
        }

        if (options.Video.ScaleMode == ScaleMode.SetOutput
            && (options.Video.Width.GetValueOrDefault() > 0 || options.Video.Height.GetValueOrDefault() > 0)
            && RequiresEvenDimensions(videoCodec))
        {
            if (options.Video.Width is > 0 and var width && width % 2 != 0)
            {
                advisories.Add($"Width {width} is odd. H.264/H.265 pipelines are usually more reliable with even dimensions.");
            }

            if (options.Video.Height is > 0 and var height && height % 2 != 0)
            {
                advisories.Add($"Height {height} is odd. H.264/H.265 pipelines are usually more reliable with even dimensions.");
            }
        }

        if (Mp4FamilyContainers.Contains(container)
            && options.Audio.Mode == StreamProcessingMode.Encode
            && string.Equals(audioCodec, "libopus", StringComparison.OrdinalIgnoreCase))
        {
            advisories.Add($"Opus inside '.{container}' is technically possible in some workflows, but compatibility is noticeably worse than AAC or MKV/WebM targets.");
        }

        if (probe is not null && options.Video.Mode == StreamProcessingMode.Copy && options.Video.ScaleMode == ScaleMode.KeepSource && options.Video.FrameRateMode == FrameRateMode.KeepSource)
        {
            if (!string.IsNullOrWhiteSpace(probe.VideoCodec) && !string.IsNullOrWhiteSpace(videoCodec)
                && string.Equals(probe.VideoCodec, videoCodec, StringComparison.OrdinalIgnoreCase))
            {
                advisories.Add($"Video Copy keeps the detected source codec '{probe.VideoCodec}' unchanged.");
            }
        }

        if (probe is not null && options.Audio.Mode == StreamProcessingMode.Copy)
        {
            if (!string.IsNullOrWhiteSpace(probe.AudioCodec) && !string.IsNullOrWhiteSpace(audioCodec)
                && string.Equals(probe.AudioCodec, audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                advisories.Add($"Audio Copy keeps the detected source codec '{probe.AudioCodec}' unchanged.");
            }
        }
    }

    private static void AppendPresetOptimizationHints(List<string> advisories, ConvertOptions options, ToolchainCapabilitiesSnapshot? capabilities, MediaProbeResult? probe)
    {
        if (capabilities is null)
        {
            return;
        }

        var balanced = BuildBalancedPreset(capabilities).Normalize();
        var efficient = BuildEfficientPreset(capabilities).Normalize();

        if (options.Video.Mode == StreamProcessingMode.Encode && !ContainsIgnoreCase(capabilities.VideoEncoders, options.Video.Codec))
        {
            advisories.Add($"This FFmpeg build does not report '{options.Video.Codec}'. The adaptive balanced preset would use '{balanced.Video.Codec}' instead.");
        }

        if (options.Audio.Mode == StreamProcessingMode.Encode && !ContainsIgnoreCase(capabilities.AudioEncoders, options.Audio.Codec))
        {
            advisories.Add($"This FFmpeg build does not report '{options.Audio.Codec}'. The adaptive presets would use '{balanced.Audio.Codec}' or '{efficient.Audio.Codec}' instead.");
        }

        if (probe is not null && options.Video.Mode == StreamProcessingMode.Copy && options.Audio.Mode == StreamProcessingMode.Copy)
        {
            var remux = BuildStreamCopyPreset(capabilities, probe, options.Container).Normalize();
            if (!string.Equals(remux.Container, options.Container, StringComparison.OrdinalIgnoreCase))
            {
                advisories.Add($"Based on the detected source streams, the adaptive remux preset would target '.{remux.Container}' instead of '.{options.Container}'.");
            }
        }
    }

    private static string SummarizePreset(ConvertOptions options, string lead)
    {
        var videoPart = options.Video.Mode switch
        {
            StreamProcessingMode.Copy => "video copy",
            StreamProcessingMode.Disable => "no video",
            _ => options.Video.Codec
        };

        var audioPart = options.Audio.Mode switch
        {
            StreamProcessingMode.Copy => "audio copy",
            StreamProcessingMode.Disable => "no audio",
            _ => options.Audio.Codec
        };

        var ratePart = options.Video.Mode != StreamProcessingMode.Encode
            ? string.Empty
            : options.Video.RateControlMode == VideoRateControlMode.ConstantQuality
                ? $", CRF {options.Video.Crf?.ToString() ?? "?"}"
                : !string.IsNullOrWhiteSpace(options.Video.Bitrate)
                    ? $", {options.Video.Bitrate}"
                    : string.Empty;

        var containerExtension = ContainerCatalog.ResolveDefaultExtension(options.Container).TrimStart('.');
        return $"{lead}: .{containerExtension}, {videoPart}{ratePart}, {audioPart}.";
    }

    private static string? ResolveDefaultAudioBitrate(string audioEncoder)
        => audioEncoder switch
        {
            var codec when string.Equals(codec, "flac", StringComparison.OrdinalIgnoreCase)
                       || codec.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(codec, "alac", StringComparison.OrdinalIgnoreCase)
                => null,
            var codec when string.Equals(codec, "libopus", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(codec, "opus", StringComparison.OrdinalIgnoreCase)
                => "128k",
            _ => "160k"
        };

    private static string ChooseEncoder(IReadOnlyList<string>? reportedEncoders, IEnumerable<string> preferredOrder, string fallback)
    {
        if (reportedEncoders is null || reportedEncoders.Count == 0)
        {
            return fallback;
        }

        foreach (var preferred in preferredOrder.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            var match = reportedEncoders.FirstOrDefault(value => value.Equals(preferred, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
            {
                return match;
            }
        }

        return reportedEncoders.FirstOrDefault() ?? fallback;
    }

    private static string ChooseMuxer(ToolchainCapabilitiesSnapshot? capabilities, IEnumerable<string?> preferredOrder, string fallback)
    {
        foreach (var preferred in preferredOrder)
        {
            var normalized = NormalizeContainer(preferred);
            if (!string.IsNullOrWhiteSpace(normalized) && HasMuxer(capabilities, normalized))
            {
                return normalized;
            }
        }

        if (capabilities?.Muxers is { Count: > 0 })
        {
            var available = ContainerCatalog.GetAvailableUserSelectableContainers(capabilities.Muxers).FirstOrDefault();
            if (available is not null)
            {
                return available.Id;
            }
        }

        return NormalizeContainer(fallback);
    }

    private static bool HasMuxer(ToolchainCapabilitiesSnapshot? capabilities, string? container)
        => capabilities?.Muxers is not { Count: > 0 }
            || ContainerCatalog.IsAvailable(container, capabilities.Muxers);

    private static bool ContainsIgnoreCase(IEnumerable<string> values, string candidate)
        => values.Any(value => value.Equals(candidate, StringComparison.OrdinalIgnoreCase));

    private static bool UsesCrfFriendlyEncoder(string codec)
        => !string.IsNullOrWhiteSpace(codec)
           && !IsHardwareEncoder(codec)
           && !codec.Equals("mjpeg", StringComparison.OrdinalIgnoreCase)
           && !codec.Equals("ffv1", StringComparison.OrdinalIgnoreCase);

    private static bool IsHardwareEncoder(string codec)
        => codec.Contains("_nvenc", StringComparison.OrdinalIgnoreCase)
           || codec.Contains("_qsv", StringComparison.OrdinalIgnoreCase)
           || codec.Contains("_amf", StringComparison.OrdinalIgnoreCase)
           || codec.Contains("_vaapi", StringComparison.OrdinalIgnoreCase);

    private static bool IsAv1Encoder(string codec)
        => codec.Contains("av1", StringComparison.OrdinalIgnoreCase);

    private static bool IsMp4FriendlyVideo(string codec)
        => Mp4PreferredVideoCodecs.Contains(codec);

    private static bool IsMp4FriendlyAudio(string codec)
        => Mp4PreferredAudioCodecs.Contains(codec);

    private static bool IsWebmFriendlyAudio(string codec)
        => WebmPreferredAudioCodecs.Contains(codec);

    private static bool NeedsCompatibilityPixelFormat(string? codec, string container)
        => !string.IsNullOrWhiteSpace(codec)
           && (codec.Contains("264", StringComparison.OrdinalIgnoreCase)
               || codec.Contains("265", StringComparison.OrdinalIgnoreCase)
               || codec.Contains("hevc", StringComparison.OrdinalIgnoreCase)
               || IsAv1Encoder(codec))
           && (Mp4FamilyContainers.Contains(container) || WebmFamilyContainers.Contains(container));

    private static bool RequiresEvenDimensions(string? codec)
        => !string.IsNullOrWhiteSpace(codec)
           && (codec.Contains("264", StringComparison.OrdinalIgnoreCase)
               || codec.Contains("265", StringComparison.OrdinalIgnoreCase)
               || codec.Contains("hevc", StringComparison.OrdinalIgnoreCase));

    private static bool IsCodecAllowed(string codec, string container, char streamType)
    {
        var normalizedContainer = NormalizeContainer(container);
        return streamType switch
        {
            'V' when normalizedContainer == "webm" => WebmPreferredVideoCodecs.Contains(codec),
            'A' when normalizedContainer == "webm" => WebmPreferredAudioCodecs.Contains(codec),
            'V' when normalizedContainer == "mov" => MovPreferredVideoCodecs.Contains(codec),
            'A' when normalizedContainer == "mov" => MovPreferredAudioCodecs.Contains(codec),
            'V' when normalizedContainer == "avi" => AviPreferredVideoCodecs.Contains(codec),
            'A' when normalizedContainer == "avi" => AviPreferredAudioCodecs.Contains(codec),
            'V' when Mp4FamilyContainers.Contains(normalizedContainer) => Mp4PreferredVideoCodecs.Contains(codec),
            'A' when Mp4FamilyContainers.Contains(normalizedContainer) => Mp4PreferredAudioCodecs.Contains(codec),
            _ => true
        };
    }

    private static string? ResolveEffectiveCodec(StreamProcessingMode mode, string configuredCodec, string? sourceCodec)
        => mode switch
        {
            StreamProcessingMode.Disable => null,
            StreamProcessingMode.Copy => string.IsNullOrWhiteSpace(sourceCodec) ? configuredCodec : sourceCodec,
            _ => configuredCodec
        };

    private static bool AreEquivalent(ConvertOptions left, ConvertOptions right)
    {
        var a = left.Normalize();
        var b = right.Normalize();
        return string.Equals(a.Container, b.Container, StringComparison.OrdinalIgnoreCase)
               && a.OverwriteMode == b.OverwriteMode
               && a.FastStart == b.FastStart
               && a.UseHardwareAcceleration == b.UseHardwareAcceleration
               && a.Video == b.Video
               && a.Audio == b.Audio;
    }

    private static string NormalizeContainer(string? container)
        => ContainerCatalog.NormalizeId(container);
}
