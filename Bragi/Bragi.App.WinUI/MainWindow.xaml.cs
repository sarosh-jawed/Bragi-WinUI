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

        if (args.SelectedItemContainer is not NavigationViewItem navigationViewItem)
        {
            return;
        }

        if (navigationViewItem.Tag is not string rawStepIndex ||
            !int.TryParse(rawStepIndex, out var stepIndex))
        {
            return;
        }

        ViewModel.GoToStep(stepIndex);
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
        ApplyNavigationItemState(StartNavigationItem, 0);
        ApplyNavigationItemState(LoadInputNavigationItem, 1);
        ApplyNavigationItemState(ReviewSubjectsNavigationItem, 2);
        ApplyNavigationItemState(PreviewResultsNavigationItem, 3);
        ApplyNavigationItemState(ExportFinishNavigationItem, 4);

        SynchronizeSelectedNavigationItem();
    }

    private void ApplyNavigationItemState(NavigationViewItem navigationViewItem, int stepIndex)
    {
        navigationViewItem.IsEnabled = ViewModel.IsStepEnabled(stepIndex);
    }

    private void SynchronizeSelectedNavigationItem()
    {
        _isUpdatingShellSelection = true;

        try
        {
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
        var targetPageType = ViewModel.CurrentPageType;

        if (ContentFrame.Content?.GetType() == targetPageType)
        {
            return;
        }

        ContentFrame.Navigate(targetPageType);
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
