using Microsoft.Extensions.DependencyInjection;
using VideoEditor.Application.Abstractions;
using VideoEditor.Application.Services;

namespace VideoEditor.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ICommandBuilder, CommandBuilder>();
        return services;
    }
}
