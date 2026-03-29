using Microsoft.Extensions.DependencyInjection;
using VidEditor.Application.Abstractions;
using VidEditor.Application.Services;

namespace VidEditor.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ICommandBuilder, CommandBuilder>();
        services.AddSingleton<IOperationRequestFactory, OperationRequestFactory>();
        return services;
    }
}
