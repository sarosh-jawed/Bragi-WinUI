using Bragi.App.WinUI.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Bragi.App.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();

        ViewModel = viewModel;
        Title = viewModel.WindowTitle;

        logger.LogInformation("Main window created.");
    }

    public MainWindowViewModel ViewModel { get; }
}
