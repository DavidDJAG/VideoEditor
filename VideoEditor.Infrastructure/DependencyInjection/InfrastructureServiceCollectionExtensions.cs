using Microsoft.Extensions.DependencyInjection;
using VideoEditor.Application.Abstractions;
using VideoEditor.Infrastructure.Execution;
using VideoEditor.Infrastructure.FileSystem;
using VideoEditor.Infrastructure.Services;
using VideoEditor.Infrastructure.Settings;

namespace VideoEditor.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ISettingsPersistence, JsonSettingsPersistence>();

        services.AddSingleton<IFfmpegService, FfmpegService>();
        services.AddSingleton<IFfprobeService, FfprobeService>();
        services.AddSingleton<IPlaybackService, PlaybackService>();

        return services;
    }
}
