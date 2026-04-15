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
