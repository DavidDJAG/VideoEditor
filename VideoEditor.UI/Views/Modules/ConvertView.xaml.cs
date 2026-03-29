using System.Windows;
using VideoEditor.UI.ViewModels.Modules;

namespace VideoEditor.UI.Views.Modules;

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
