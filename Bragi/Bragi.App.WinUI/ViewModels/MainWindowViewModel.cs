using System;
using Bragi.App.WinUI.Startup;
using Microsoft.Extensions.Configuration;

namespace Bragi.App.WinUI.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(
        IConfiguration configuration,
        BragiStartupContext startupContext)
    {
        var appName = configuration["Bragi:AppName"] ?? "Bragi";

        WindowTitle = appName;
        AppTitle = $"{appName} startup foundation";
        StatusMessage = "Bragi launched through dependency injection. Configuration loading and startup logging are active.";

        ConfigSummary =
            $"Packaged config: {startupContext.PackagedConfigPath}{Environment.NewLine}" +
            $"Local override: {startupContext.LocalConfigPath}";

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
