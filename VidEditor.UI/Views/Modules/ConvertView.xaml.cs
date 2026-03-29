using System.Windows;
using VidEditor.UI.ViewModels.Modules;

namespace VidEditor.UI.Views.Modules;

public partial class ConvertView : System.Windows.Controls.UserControl
{
    public ConvertView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ModulesWorkbenchViewModel viewModel)
        {
            await viewModel.EnsureConvertCapabilitiesInitializedAsync();
            await viewModel.EnsureConvertInputContextInitializedAsync();
        }
    }
}
