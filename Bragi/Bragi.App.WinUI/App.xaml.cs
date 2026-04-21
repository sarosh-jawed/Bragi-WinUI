using System;
using System.IO;
using Bragi.App.WinUI.Startup;
using Bragi.App.WinUI.ViewModels;
using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;
using Bragi.Infrastructure.Extraction;
using Bragi.Infrastructure.Ingestion;
using Bragi.Infrastructure.Categorization;
using Bragi.Infrastructure.Export;
using Bragi.Application.Workflow;
using Bragi.Infrastructure.Workflow;
using Bragi.App.WinUI.ViewModels.Base;

namespace Bragi.App.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _mainWindow;
    private readonly ILogger<App> _logger;
    private const string DefaultLogsRootToken = "%DOCUMENTS%\\Bragi\\Logs";
    public static IHost AppHost { get; } = CreateHostBuilder().Build();

    public static App CurrentApp =>
    Microsoft.UI.Xaml.Application.Current as App
    ?? throw new InvalidOperationException("The current application is not Bragi.App.WinUI.App.");

    public static Window? MainAppWindow => CurrentApp._mainWindow;

    public static T GetService<T>()
        where T : notnull
    {
        return AppHost.Services.GetRequiredService<T>();
    }

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

            var configProvider = AppHost.Services.GetRequiredService<IConfigProvider>();
            var config = await configProvider.LoadAsync();

            _logger.LogInformation(
                "Startup configuration loaded successfully. CategoryRuleCount={CategoryRuleCount} PackagedConfigPath={PackagedConfigPath} LocalConfigPath={LocalConfigPath}",
                config.CategoryRules.Count,
                GetPackagedConfigPath(),
                GetLocalConfigPath());

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
            // Create the log root eagerly so support logs are available even for early startup failures.
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                var pathTokenResolver = new PathTokenResolver();
                var logsRoot = pathTokenResolver.Resolve(DefaultLogsRootToken);

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
                services.AddSingleton<PathTokenResolver>();
                services.AddSingleton<BragiConfigLoader>();
                services.AddSingleton<BragiConfigValidator>();
                services.AddSingleton<IConfigProvider, ConfigProvider>();

                services.AddSingleton(sp => sp.GetRequiredService<IConfigProvider>().GetRequiredConfig());

                services.AddSingleton<IInputIngestService, InputIngestService>();
                services.AddSingleton<ISubjectExtractionService, SubjectExtractionService>();

                services.AddSingleton<SubjectNormalizationHelper>();
                services.AddSingleton<KeywordMatcher>();
                services.AddSingleton<ExclusionMatcher>();
                services.AddSingleton<ICategorizationService, CategorizationService>();

                services.AddSingleton<TextBodyBuilder>();
                services.AddSingleton<RunSummaryBuilder>();
                services.AddSingleton<ITextExportService, TextExportService>();

                services.AddSingleton<WizardSessionStore>();
                services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
                services.AddSingleton<IStepNavigationService, StepNavigationService>();

                services.AddSingleton(sp =>
                {
                    var pathTokenResolver = sp.GetRequiredService<PathTokenResolver>();
                    var config = sp.GetRequiredService<BragiConfig>();
                    var logsRoot = pathTokenResolver.Resolve(DefaultLogsRootToken);
                    return new BragiStartupContext(
                        packagedConfigPath,
                        localConfigPath,
                        logsRoot,
                        config.Output.RootPath);
                });

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<LoadInputPageViewModel>();
                services.AddSingleton<ReviewSubjectsPageViewModel>();
                services.AddSingleton<PreviewResultsPageViewModel>();
                services.AddSingleton<ExportFinishPageViewModel>();
                services.AddSingleton<MainWindow>();
            });
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
