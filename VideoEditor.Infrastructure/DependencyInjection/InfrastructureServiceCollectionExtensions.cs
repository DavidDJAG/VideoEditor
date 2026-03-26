using Microsoft.Extensions.DependencyInjection;
using VideoEditor.Application.Abstractions;
using VideoEditor.Application.Services;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.FileSystem;
using VideoEditor.Infrastructure.Services;
using VideoEditor.Infrastructure.Settings;
using VideoEditor.Infrastructure.Toolchain;

namespace VideoEditor.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ISettingsPersistence, JsonSettingsPersistence>();
        services.AddSingleton<IToolchainResolver, ToolchainResolver>();

        services.AddSingleton<IFfmpegService, FfmpegService>();
        services.AddSingleton<IFfprobeService, FfprobeService>();
        services.AddSingleton<IPlaybackService, PlaybackService>();
        services.AddSingleton<IToolchainCapabilitiesService, ToolchainCapabilitiesService>();

        services.AddSingleton<IJobStore>(_ =>
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "jobs.db");
            return new SqliteJobStore(dbPath);
        });
        services.AddSingleton<IJobExecutionService, FfmpegJobExecutionService>();
        services.AddSingleton<IJobQueueService, InMemoryJobQueueService>();

        return services;
    }
}
