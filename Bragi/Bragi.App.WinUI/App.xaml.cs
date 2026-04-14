using System;
using System.IO;
using System.Linq;
using Bragi.App.WinUI.Startup;
using Bragi.App.WinUI.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;

namespace Bragi.App.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _mainWindow;
    private readonly ILogger<App> _logger;

    public static IHost AppHost { get; } = CreateHostBuilder().Build();

    public App()
    {
        InitializeComponent();

        UnhandledException += OnUnhandledException;

        _logger = AppHost.Services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("Bragi application object created.");
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        try
        {
            await AppHost.StartAsync();

            var configuration = AppHost.Services.GetRequiredService<IConfiguration>();
            ValidateStartupConfiguration(configuration);

            _logger.LogInformation("Startup configuration loaded and validated successfully.");

            _mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            _mainWindow.Closed += OnMainWindowClosed;
            _mainWindow.Activate();

            _logger.LogInformation("Main window activated.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Application launch failed.");
            throw;
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        var packagedConfigPath = GetPackagedConfigPath();
        var localConfigPath = GetLocalConfigPath();

        return Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile(packagedConfigPath, optional: false, reloadOnChange: false);
                config.AddJsonFile(localConfigPath, optional: true, reloadOnChange: false);
            })
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                var logsRoot = ExpandSpecialPathTokens(
                    context.Configuration["Bragi:Paths:LogsRoot"] ?? "%LOCALAPPDATA%\\Bragi\\Logs");

                Directory.CreateDirectory(logsRoot);

                loggerConfiguration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.File(
                        path: Path.Combine(logsRoot, "bragi-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        shared: true);
            })
            .ConfigureServices((context, services) =>
            {
                var logsRoot = ExpandSpecialPathTokens(
                    context.Configuration["Bragi:Paths:LogsRoot"] ?? "%LOCALAPPDATA%\\Bragi\\Logs");

                var outputRoot = ExpandSpecialPathTokens(
                    context.Configuration["Bragi:Paths:OutputRoot"] ?? "%LOCALAPPDATA%\\Bragi\\Output");

                services.AddSingleton(new BragiStartupContext(
                    packagedConfigPath,
                    localConfigPath,
                    logsRoot,
                    outputRoot));

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            });
    }

    private static void ValidateStartupConfiguration(IConfiguration configuration)
    {
        var requiredKeys = new[]
        {
            "Bragi:AppName",
            "Bragi:Paths:LogsRoot",
            "Bragi:Paths:OutputRoot"
        };

        var missingKeys = requiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToArray();

        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Startup configuration is missing required keys: {string.Join(", ", missingKeys)}");
        }
    }

    private static string GetPackagedConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "config.json");
    }

    private static string GetLocalConfigPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bragi",
            "config.local.json");
    }

    private static string ExpandSpecialPathTokens(string path)
    {
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        return path
            .Replace("%LOCALAPPDATA%", localAppDataPath, StringComparison.OrdinalIgnoreCase)
            .Replace("%USERPROFILE%", userProfilePath, StringComparison.OrdinalIgnoreCase)
            .Replace("%DOCUMENTS%", documentsPath, StringComparison.OrdinalIgnoreCase);
    }

    private async void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        _logger.LogInformation("Main window closed. Stopping host.");

        try
        {
            await AppHost.StopAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            AppHost.Dispose();
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _logger.LogCritical(e.Exception, "Unhandled exception occurred.");
    }
}
