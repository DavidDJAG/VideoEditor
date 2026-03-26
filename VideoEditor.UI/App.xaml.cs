using Microsoft.Extensions.DependencyInjection;
using VideoEditor.Application.DependencyInjection;
using VideoEditor.Infrastructure.DependencyInjection;
using VideoEditor.UI.ViewModels;
using VideoEditor.UI.ViewModels.Modules;

namespace VideoEditor.UI;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
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
        services.AddTransient<QueueViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }
}
