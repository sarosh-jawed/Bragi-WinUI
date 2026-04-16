using System;
using System.IO;
using System.Threading.Tasks;
using Bragi.App.WinUI.ViewModels.Base;
using Bragi.Application.Contracts;
using Bragi.Application.Workflow;
using Microsoft.Extensions.Logging;
using Bragi.Application.Errors;

namespace Bragi.App.WinUI.ViewModels;

public sealed class LoadInputPageViewModel : ObservableObject
{
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly WizardSessionStore _wizardSessionStore;
    private readonly ILogger<LoadInputPageViewModel> _logger;

    private string _selectedInputFilePath = string.Empty;
    private string _detectedInputKind = "Not loaded";
    private string _statusMessage = "Choose a .txt or .csv file to begin.";
    private int _totalRecordsRead;
    private int _extractedSubjectCount;
    private int _blankOrIgnoredCount;
    private int _duplicateCount;
    private int _parseWarningCount;
    private bool _isBusy;

    public LoadInputPageViewModel(
        IWorkflowOrchestrator workflowOrchestrator,
        WizardSessionStore wizardSessionStore,
        ILogger<LoadInputPageViewModel> logger)
    {
        _workflowOrchestrator = workflowOrchestrator ?? throw new ArgumentNullException(nameof(workflowOrchestrator));
        _wizardSessionStore = wizardSessionStore ?? throw new ArgumentNullException(nameof(wizardSessionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _wizardSessionStore.SessionChanged += OnSessionChanged;
        RefreshFromSession();
    }

    public string SelectedInputFilePath
    {
        get => _selectedInputFilePath;
        private set
        {
            if (SetProperty(ref _selectedInputFilePath, value))
            {
                OnPropertyChanged(nameof(InputFilePath));
                OnPropertyChanged(nameof(FileNameDisplay));
                OnPropertyChanged(nameof(CanReloadExtraction));
            }
        }
    }

    public string InputFilePath => SelectedInputFilePath;

    public string DetectedInputKind
    {
        get => _detectedInputKind;
        private set => SetProperty(ref _detectedInputKind, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int TotalRecordsRead
    {
        get => _totalRecordsRead;
        private set => SetProperty(ref _totalRecordsRead, value);
    }

    public int ExtractedSubjectCount
    {
        get => _extractedSubjectCount;
        private set
        {
            if (SetProperty(ref _extractedSubjectCount, value))
            {
                OnPropertyChanged(nameof(HasExtraction));
            }
        }
    }

    public int BlankOrIgnoredCount
    {
        get => _blankOrIgnoredCount;
        private set => SetProperty(ref _blankOrIgnoredCount, value);
    }

    public int DuplicateCount
    {
        get => _duplicateCount;
        private set => SetProperty(ref _duplicateCount, value);
    }

    public int ParseWarningCount
    {
        get => _parseWarningCount;
        private set => SetProperty(ref _parseWarningCount, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanReloadExtraction));
            }
        }
    }

    public bool HasExtraction => ExtractedSubjectCount > 0 || TotalRecordsRead > 0;

    public bool CanReloadExtraction =>
        !IsBusy && !string.IsNullOrWhiteSpace(SelectedInputFilePath);

    public string FileNameDisplay =>
        string.IsNullOrWhiteSpace(SelectedInputFilePath)
            ? "No file selected"
            : Path.GetFileName(SelectedInputFilePath);

    public async Task LoadInputAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            StatusMessage = "Please choose a valid input file.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Loading input and extracting subjects...";

            var extractionResult = await _workflowOrchestrator.PreviewExtractionAsync(filePath);

            RefreshFromSession();

            StatusMessage =
                $"Loaded {extractionResult.ExtractedCount} extracted subjects from {Path.GetFileName(filePath)}.";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Input loading was cancelled for file {FilePath}.", filePath);
            RefreshFromSession();
            StatusMessage = "Input loading was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load input file {FilePath}.", filePath);
            RefreshFromSession();
            StatusMessage = UserMessageHelper.ForInputLoad(ex);
        }
        finally
        {
            IsBusy = _wizardSessionStore.State.IsBusy;
            OnPropertyChanged(nameof(HasExtraction));
            OnPropertyChanged(nameof(FileNameDisplay));
            OnPropertyChanged(nameof(InputFilePath));
            OnPropertyChanged(nameof(CanReloadExtraction));
        }
    }

    public void RefreshFromSession()
    {
        SelectedInputFilePath = _wizardSessionStore.SelectedInputFile ?? string.Empty;

        DetectedInputKind =
            _wizardSessionStore.InputKind == Domain.Enums.InputFileKind.Unknown
                ? "Not loaded"
                : _wizardSessionStore.InputKind.ToString();

        var extractionResult = _wizardSessionStore.ExtractedSubjects;

        TotalRecordsRead = extractionResult?.TotalRecordsRead ?? 0;
        ExtractedSubjectCount = extractionResult?.ExtractedCount ?? 0;
        BlankOrIgnoredCount = extractionResult?.BlankOrIgnoredCount ?? 0;
        DuplicateCount = extractionResult?.DuplicateCount ?? 0;
        ParseWarningCount = extractionResult?.ParseWarningCount ?? 0;
        IsBusy = _wizardSessionStore.State.IsBusy;

        OnPropertyChanged(nameof(HasExtraction));
        OnPropertyChanged(nameof(FileNameDisplay));
        OnPropertyChanged(nameof(InputFilePath));
        OnPropertyChanged(nameof(CanReloadExtraction));
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        RefreshFromSession();
    }
}
