using System;
using System.IO;
using System.Threading.Tasks;
using Bragi.App.WinUI.ViewModels.Base;
using Bragi.Application.Contracts;
using Bragi.Application.Errors;
using Bragi.Application.Workflow;
using Microsoft.Extensions.Logging;

namespace Bragi.App.WinUI.ViewModels;

public sealed class LoadInputPageViewModel : ObservableObject
{
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly WizardSessionStore _wizardSessionStore;
    private readonly ILogger<LoadInputPageViewModel> _logger;

    private string _selectedInputFilePath = string.Empty;
    private string _detectedInputKind = "Not loaded";
    private string _statusMessage = "Choose a .txt or .csv file to begin.";
    private string _loadProgressText = "Ready to load a source file.";
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
                OnPropertyChanged(nameof(CanChooseFile));
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

    public string LoadProgressText
    {
        get => _loadProgressText;
        private set => SetProperty(ref _loadProgressText, value);
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
                OnPropertyChanged(nameof(CanChooseFile));
                OnPropertyChanged(nameof(IsIdle));
            }
        }
    }

    public bool HasExtraction => ExtractedSubjectCount > 0 || TotalRecordsRead > 0;

    public bool IsIdle => !IsBusy;

    public bool CanChooseFile => !IsBusy;

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
            LoadProgressText = "Reading file and extracting subjects. Large CSV files may take a moment.";
            StatusMessage = "Loading input and extracting subjects...";

            var extractionResult = await _workflowOrchestrator.PreviewExtractionAsync(filePath);

            RefreshFromSession();

            LoadProgressText = "Load completed.";
            StatusMessage =
                $"Loaded {extractionResult.ExtractedCount} extracted subjects from {Path.GetFileName(filePath)}.";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Input loading was cancelled for file {FilePath}.", filePath);
            RefreshFromSession();
            LoadProgressText = "Load cancelled.";
            StatusMessage = "Input loading was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load input file {FilePath}.", filePath);
            RefreshFromSession();
            LoadProgressText = "Load failed.";
            StatusMessage = UserMessageHelper.ForInputLoad(ex);
        }
        finally
        {
            IsBusy = _wizardSessionStore.State.IsBusy;
            OnPropertyChanged(nameof(HasExtraction));
            OnPropertyChanged(nameof(FileNameDisplay));
            OnPropertyChanged(nameof(InputFilePath));
            OnPropertyChanged(nameof(CanReloadExtraction));
            OnPropertyChanged(nameof(CanChooseFile));
            OnPropertyChanged(nameof(IsIdle));
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

        if (!IsBusy && extractionResult is null)
        {
            LoadProgressText = "Ready to load a source file.";
        }

        OnPropertyChanged(nameof(HasExtraction));
        OnPropertyChanged(nameof(FileNameDisplay));
        OnPropertyChanged(nameof(InputFilePath));
        OnPropertyChanged(nameof(CanReloadExtraction));
        OnPropertyChanged(nameof(CanChooseFile));
        OnPropertyChanged(nameof(IsIdle));
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        RefreshFromSession();
    }
}
