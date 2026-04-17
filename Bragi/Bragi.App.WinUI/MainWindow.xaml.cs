using System;
using System.ComponentModel;
using Bragi.App.WinUI.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Bragi.App.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private bool _isUpdatingShellSelection;

    public MainWindow(
        MainWindowViewModel viewModel,
        ILogger<MainWindow> logger)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeComponent();

        Title = ViewModel.WindowTitle;

        Activated += MainWindow_OnActivated;
        Closed += MainWindow_OnClosed;
        ViewModel.PropertyChanged += ViewModel_OnPropertyChanged;
    }

    public MainWindowViewModel ViewModel { get; }

    private void RootGrid_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize();
        RefreshShellNavigationState();

        ContentFrame.CacheSize = 5;

        NavigateToCurrentPage();

        _logger.LogInformation("Bragi shell loaded.");
    }

    private void ShellNavigationView_OnSelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (_isUpdatingShellSelection)
        {
            return;
        }

        if (args.SelectedItemContainer is not NavigationViewItem selectedItem)
        {
            return;
        }

        var requestedStepTag = selectedItem.Tag?.ToString();

        if (string.IsNullOrWhiteSpace(requestedStepTag))
        {
            return;
        }

        switch (requestedStepTag)
        {
            case "Start":
                NavigateFromMenuToStep(0);
                break;

            case "LoadInput":
                NavigateFromMenuToStep(1);
                break;

            case "ReviewSubjects":
                NavigateFromMenuToStep(2);
                break;

            case "PreviewResults":
                NavigateFromMenuToStep(3);
                break;

            case "ExportFinish":
                NavigateFromMenuToStep(4);
                break;
        }
    }

    private void NavigateFromMenuToStep(int targetStepIndex)
    {
        ViewModel.GoToStep(targetStepIndex);
        RefreshShellNavigationState();
        NavigateToCurrentPage();
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MovePrevious();
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MoveNext();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelBusyOperation();
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshShellNavigationState();
        NavigateToCurrentPage();
    }

    private void RefreshShellNavigationState()
    {
        _isUpdatingShellSelection = true;

        try
        {
            StartNavigationItem.IsEnabled = ViewModel.IsStepEnabled(0);
            LoadInputNavigationItem.IsEnabled = ViewModel.IsStepEnabled(1);
            ReviewSubjectsNavigationItem.IsEnabled = ViewModel.IsStepEnabled(2);
            PreviewResultsNavigationItem.IsEnabled = ViewModel.IsStepEnabled(3);
            ExportFinishNavigationItem.IsEnabled = ViewModel.IsStepEnabled(4);

            ShellNavigationView.SelectedItem = ViewModel.CurrentStepIndex switch
            {
                0 => StartNavigationItem,
                1 => LoadInputNavigationItem,
                2 => ReviewSubjectsNavigationItem,
                3 => PreviewResultsNavigationItem,
                4 => ExportFinishNavigationItem,
                _ => StartNavigationItem
            };
        }
        finally
        {
            _isUpdatingShellSelection = false;
        }
    }

    private void NavigateToCurrentPage()
    {
        var targetPage = ViewModel.CurrentPageType;

        if (ContentFrame.CurrentSourcePageType != targetPage)
        {
            ContentFrame.Navigate(targetPage);
        }
    }

    private void MainWindow_OnActivated(object sender, WindowActivatedEventArgs args)
    {
        _logger.LogInformation("Main window activated.");
    }

    private void MainWindow_OnClosed(object sender, WindowEventArgs args)
    {
        ViewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
        Activated -= MainWindow_OnActivated;
        Closed -= MainWindow_OnClosed;
    }
}
