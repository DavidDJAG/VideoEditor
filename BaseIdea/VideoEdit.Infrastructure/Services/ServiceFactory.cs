using VideoEdit.Application.Services;
using VideoEdit.Domain.Models;

namespace VideoEdit.Infrastructure.Services;

public sealed class ServiceFactory
{
    private readonly string _dataDirectory;
    private AppSettings? _settings;

    public ServiceFactory(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
    }

    public async Task<ServiceBundle> CreateAsync(CancellationToken cancellationToken)
    {
        var settingsStore = new SettingsStore(_dataDirectory);
        _settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var toolResolver = new ToolResolver(() => _settings?.FfmpegDirectory);
        var commandBuilder = new FfmpegArgumentBuilder();
        var processRunner = new FfmpegProcessRunner();
        var ffmpegService = new FfmpegService(commandBuilder, processRunner, toolResolver);
        var ffprobeService = new FfprobeService(processRunner, toolResolver);
        var concatCompatibilityAnalyzer = new ConcatCompatibilityAnalyzer(ffprobeService);
        var logStore = new JsonLogStore(_dataDirectory);
        var queueService = new JobQueueService(ffmpegService, logStore);
        var validator = new MediaJobValidator();

        return new ServiceBundle(
            ffmpegService,
            ffprobeService,
            concatCompatibilityAnalyzer,
            queueService,
            logStore,
            settingsStore,
            validator,
            toolResolver,
            _settings);
    }
}
