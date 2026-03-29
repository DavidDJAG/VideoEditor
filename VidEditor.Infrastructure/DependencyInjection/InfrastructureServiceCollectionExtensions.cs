using Microsoft.Extensions.DependencyInjection;
using VidEditor.Application.Abstractions;
using VidEditor.Application.Services;
using VidEditor.Infrastructure.Execution;
using VidEditor.Infrastructure.FileSystem;
using VidEditor.Infrastructure.Services;
using VidEditor.Infrastructure.Settings;
using VidEditor.Infrastructure.Toolchain;

namespace VidEditor.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ISettingsPersistence, JsonSettingsPersistence>();
        services.AddSingleton<IToolchainResolver, ToolchainResolver>();
        services.AddSingleton<IConcatCompatibilityService, ConcatCompatibilityService>();

        services.AddSingleton<IFfmpegService, FfmpegService>();
        services.AddSingleton<IFfprobeService, FfprobeService>();
        services.AddSingleton<IPlaybackService, PlaybackService>();
        services.AddSingleton<IToolchainCapabilitiesService, ToolchainCapabilitiesService>();

        services.AddSingleton<IJobStore>(_ =>
        {
            var jobsDirectory = Path.Combine(AppContext.BaseDirectory, "data", "jobs");
            return new JsonJobStore(jobsDirectory);
        });
        services.AddSingleton<IJobExecutionService, FfmpegJobExecutionService>();
        services.AddSingleton<IJobQueueService, InMemoryJobQueueService>();

        return services;
    }
}
