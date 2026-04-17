using System.Threading.Tasks;
using Bragi.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Bragi.App.WinUI.Pages;

public sealed partial class PreviewResultsPage : Page
{
    public PreviewResultsPage()
    {
        ViewModel = App.GetService<PreviewResultsPageViewModel>();
        InitializeComponent();

        // Keep the page instance alive between navigations so the already-built
        // preview visual tree can be reused instead of recreated on every visit.
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
    }

    public PreviewResultsPageViewModel ViewModel { get; }

    private async void Page_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.EnsurePreviewAsync();
    }

    private async void GeneratePreviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.EnsurePreviewAsync();
    }
}
