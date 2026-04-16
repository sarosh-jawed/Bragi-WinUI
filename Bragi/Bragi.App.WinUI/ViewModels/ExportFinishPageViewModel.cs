using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bragi.App.WinUI.Startup;
using Bragi.App.WinUI.ViewModels.Base;
using Bragi.Application.Contracts;
using Bragi.Application.Workflow;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Bragi.Application.Errors;

namespace Bragi.App.WinUI.ViewModels;

public sealed class ExportFinishPageViewModel : ObservableObject
{
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly WizardSessionStore _wizardSessionStore;
    private readonly BragiStartupContext _startupContext;
    private readonly ILogger<ExportFinishPageViewModel> _logger;

    private string _selectedOutputFolder = string.Empty;
    private string _statusMessage = "Generate output files when preview results are ready.";
    private string _runSummaryText = "No run summary has been generated yet.";

    public ExportFinishPageViewModel(
        IWorkflowOrchestrator workflowOrchestrator,
        WizardSessionStore wizardSessionStore,
        BragiStartupContext startupContext,
        ILogger<ExportFinishPageViewModel> logger)
    {
        _workflowOrchestrator = workflowOrchestrator ?? throw new ArgumentNullException(nameof(workflowOrchestrator));
        _wizardSessionStore = wizardSessionStore ?? throw new ArgumentNullException(nameof(wizardSessionStore));
        _startupContext = startupContext ?? throw new ArgumentNullException(nameof(startupContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        GeneratedFiles = new ObservableCollection<string>();

        _wizardSessionStore.SessionChanged += OnSessionChanged;
        RefreshFromSession();
    }

    public ObservableCollection<string> GeneratedFiles { get; }

    public string SelectedOutputFolder
    {
        get => _selectedOutputFolder;
        private set => SetProperty(ref _selectedOutputFolder, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string RunSummaryText
    {
        get => _runSummaryText;
        private set => SetProperty(ref _runSummaryText, value);
    }

    public string LogsFolderPath => _startupContext.LogsRoot;

    public bool CanGenerateOutput =>
        !_wizardSessionStore.State.IsBusy &&
        _wizardSessionStore.LastCategorizationResult is not null &&
        !string.IsNullOrWhiteSpace(_wizardSessionStore.SelectedInputFile) &&
        !string.IsNullOrWhiteSpace(SelectedOutputFolder);

    public bool HasGeneratedFiles => GeneratedFiles.Count > 0;

    public bool CanOpenOutputFolder =>
        HasGeneratedFiles || !string.IsNullOrWhiteSpace(SelectedOutputFolder);

    public bool CanOpenLogFolder =>
        !string.IsNullOrWhiteSpace(LogsFolderPath);

    public Visibility ExportResultsVisibility =>
        HasGeneratedFiles ? Visibility.Visible : Visibility.Collapsed;

    public void SetSelectedOutputFolder(string selectedOutputFolder)
    {
        if (string.IsNullOrWhiteSpace(selectedOutputFolder))
        {
            return;
        }

        _wizardSessionStore.SetSelectedOutputFolder(selectedOutputFolder.Trim());
        RefreshFromSession();

        StatusMessage = "Output folder updated.";
    }

    public async Task ExecuteExportAsync()
    {
        var sourceFilePath = _wizardSessionStore.SelectedInputFile;

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            StatusMessage = "Select and load an input file before exporting.";
            return;
        }

        if (_wizardSessionStore.LastCategorizationResult is null)
        {
            StatusMessage = "Generate preview results before exporting.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedOutputFolder))
        {
            StatusMessage = "Choose an output folder before generating files.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(SelectedOutputFolder))
        {
            _wizardSessionStore.SetSelectedOutputFolder(SelectedOutputFolder);
        }

        try
        {
            StatusMessage = "Generating export files...";

            await _workflowOrchestrator.ExecuteAsync(sourceFilePath);

            RefreshFromSession();
            StatusMessage = "Export completed successfully.";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Export operation was cancelled.");
            RefreshFromSession();
            StatusMessage = "Export was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export operation failed.");
            RefreshFromSession();
            StatusMessage = UserMessageHelper.ForExport(ex);
        }
        finally
        {
            RaiseComputedPropertyChanges();
        }
    }

    public void OpenOutputFolder()
    {
        var targetFolder =
            GeneratedFiles.FirstOrDefault() is { Length: > 0 } firstGeneratedFile
                ? Path.GetDirectoryName(firstGeneratedFile)
                : SelectedOutputFolder;

        OpenFolder(targetFolder);
    }

    public void OpenLogFolder()
    {
        OpenFolder(LogsFolderPath);
    }

    public void RefreshFromSession()
    {
        SelectedOutputFolder =
            _wizardSessionStore.SelectedOutputFolder
            ?? string.Empty;

        GeneratedFiles.Clear();

        foreach (var filePath in _wizardSessionStore.GeneratedFiles)
        {
            GeneratedFiles.Add(filePath);
        }

        RunSummaryText = BuildRunSummaryText();

        RaiseComputedPropertyChanges();
    }

    private string BuildRunSummaryText()
    {
        var runSummary = _wizardSessionStore.LastRunSummary;

        if (runSummary is null)
        {
            return "No run summary has been generated yet.";
        }

        var builder = new StringBuilder();

        builder.AppendLine($"Input file: {runSummary.SourceFile}");
        builder.AppendLine($"Completed at: {runSummary.RunCompletedAtUtc:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"Extracted subjects: {runSummary.ExtractedSubjectCount}");
        builder.AppendLine($"Categorized assignments: {runSummary.CategorizedAssignmentCount}");
        builder.AppendLine($"Uncategorized subjects: {runSummary.UncategorizedSubjectCount}");
        builder.AppendLine($"Blank or ignored: {runSummary.BlankOrIgnoredCount}");
        builder.AppendLine($"Parse warnings: {runSummary.ParseWarningCount}");
        builder.AppendLine($"Duplicates: {runSummary.DuplicateCount}");

        return builder.ToString().TrimEnd();
    }

    private void OpenFolder(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            StatusMessage = "No folder is available to open.";
            return;
        }

        try
        {
            Directory.CreateDirectory(folderPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder {FolderPath}.", folderPath);
            StatusMessage = UserMessageHelper.ForFolderOpen(ex);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        RefreshFromSession();
    }

    private void RaiseComputedPropertyChanges()
    {
        OnPropertyChanged(nameof(CanGenerateOutput));
        OnPropertyChanged(nameof(HasGeneratedFiles));
        OnPropertyChanged(nameof(CanOpenOutputFolder));
        OnPropertyChanged(nameof(CanOpenLogFolder));
        OnPropertyChanged(nameof(ExportResultsVisibility));
    }
}
