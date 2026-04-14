using System;
using Bragi.App.WinUI.Startup;
using Bragi.Application.Configuration;

namespace Bragi.App.WinUI.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(
        BragiConfig config,
        BragiStartupContext startupContext)
    {
        WindowTitle = "Bragi";
        AppTitle = "Bragi startup foundation";
        StatusMessage =
            $"Bragi launched through dependency injection. Configuration loading, validation, and startup logging are active. Loaded {config.CategoryRules.Count} category rules.";

        ConfigSummary =
            $"Packaged config: {startupContext.PackagedConfigPath}{Environment.NewLine}" +
            $"Local override: {startupContext.LocalConfigPath}{Environment.NewLine}" +
            $"Category rules loaded: {config.CategoryRules.Count}";

        PathSummary =
            $"Logs root: {startupContext.LogsRoot}{Environment.NewLine}" +
            $"Default output root: {startupContext.OutputRoot}";
    }

    public string WindowTitle { get; }

    public string AppTitle { get; }

    public string StatusMessage { get; }

    public string ConfigSummary { get; }

    public string PathSummary { get; }
}
