using VideoEdit.Infrastructure.Services;

namespace VideoEdit.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoEdit");
        var services = new ServiceFactory(dataDirectory).CreateAsync(CancellationToken.None).GetAwaiter().GetResult();

        System.Windows.Forms.Application.Run(new MainForm(services));
    }
}
