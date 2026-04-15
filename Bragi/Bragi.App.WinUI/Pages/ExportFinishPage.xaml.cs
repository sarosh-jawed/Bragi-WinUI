using System;
using Bragi.App.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Bragi.App.WinUI.Pages;

public sealed partial class ExportFinishPage : Page
{
    public ExportFinishPage()
    {
        ViewModel = App.GetService<ExportFinishPageViewModel>();
        InitializeComponent();
    }

    public ExportFinishPageViewModel ViewModel { get; }

    private void Page_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshFromSession();
    }

    private async void ChooseOutputFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var mainWindow = App.MainAppWindow;
        if (mainWindow is null)
        {
            return;
        }

        InitializeWithWindow.Initialize(
            picker,
            WindowNative.GetWindowHandle(mainWindow));

        var folder = await picker.PickSingleFolderAsync();

        if (folder is null)
        {
            return;
        }

        ViewModel.SetSelectedOutputFolder(folder.Path);
    }

    private async void GenerateOutputButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExecuteExportAsync();
    }

    private void OpenOutputFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenOutputFolder();
    }

    private void OpenLogFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenLogFolder();
    }
}
