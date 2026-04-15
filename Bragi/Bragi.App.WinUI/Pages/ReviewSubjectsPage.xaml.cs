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
    }

    public ReviewSubjectsPageViewModel ViewModel { get; }

    private void MarkReviewCompleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MarkReviewComplete();
    }
}
