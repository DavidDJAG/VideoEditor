using Microsoft.Extensions.DependencyInjection;
using System.Windows.Threading;
using VidEditor.Application.DependencyInjection;
using VidEditor.Application.Abstractions;
using VidEditor.Infrastructure.DependencyInjection;
using VidEditor.UI.ViewModels;
using VidEditor.UI.ViewModels.Modules;

namespace VidEditor.UI;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        await Dispatcher.Yield(DispatcherPriority.Background);

        var queueService = _serviceProvider.GetRequiredService<IJobQueueService>();
        var dashboardViewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();

        try
        {
            await queueService.InitializeAsync();
            await dashboardViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            dashboardViewModel.ApplyError(ex.Message);
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _serviceProvider?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddApplication();
        services.AddInfrastructure();

        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();

        services.AddTransient<TrimViewModel>();
        services.AddTransient<TranscodeViewModel>();
        services.AddTransient<ConcatViewModel>();
        services.AddTransient<ProbeViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<QueueViewModel>();
        services.AddTransient<SplitAvViewModel>();
        services.AddTransient<ModulesWorkbenchViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }
}
