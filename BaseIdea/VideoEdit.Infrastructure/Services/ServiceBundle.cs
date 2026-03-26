using VideoEdit.Application.Abstractions;
using VideoEdit.Application.Services;
using VideoEdit.Domain.Models;

namespace VideoEdit.Infrastructure.Services;

public sealed record ServiceBundle(
    IFfmpegService FfmpegService,
    IFfprobeService FfprobeService,
    IConcatCompatibilityAnalyzer ConcatCompatibilityAnalyzer,
    IJobQueueService JobQueueService,
    ILogStore LogStore,
    ISettingsStore SettingsStore,
    MediaJobValidator Validator,
    IToolResolver ToolResolver,
    AppSettings Settings);
