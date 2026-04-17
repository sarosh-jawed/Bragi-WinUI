using Bragi.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Bragi.App.WinUI.Pages;

public sealed partial class ReviewSubjectsPage : Page
{
    public ReviewSubjectsPage()
    {
        ViewModel = App.GetService<ReviewSubjectsPageViewModel>();
        InitializeComponent();

        // Keep the page instance alive between navigations so WinUI does not
        // recreate and rebind the review list every time the user moves back
        // and forth through the wizard.
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
    }

    public ReviewSubjectsPageViewModel ViewModel { get; }

    private void MarkReviewCompleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MarkReviewComplete();
    }
}
