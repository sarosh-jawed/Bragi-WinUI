using System;
using Bragi.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Bragi.App.WinUI.Pages;

public sealed partial class LoadInputPage : Page
{
    public LoadInputPage()
    {
        ViewModel = App.GetService<LoadInputPageViewModel>();
        InitializeComponent();
    }

    public LoadInputPageViewModel ViewModel { get; }

    private async void ChooseFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".csv");

        var mainWindow = App.MainAppWindow;
        if (mainWindow is null)
        {
            return;
        }

        InitializeWithWindow.Initialize(
            picker,
            WindowNative.GetWindowHandle(mainWindow));

        var file = await picker.PickSingleFileAsync();

        if (file is null)
        {
            return;
        }

        await ViewModel.LoadInputAsync(file.Path);
    }

    private async void ReloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.SelectedInputFilePath))
        {
            return;
        }

        await ViewModel.LoadInputAsync(ViewModel.SelectedInputFilePath);
    }
}
