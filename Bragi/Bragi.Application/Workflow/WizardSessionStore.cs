using System;
using System.Linq;
using System.Threading;
using Bragi.Domain.Enums;
using Bragi.Domain.Results;

namespace Bragi.Application.Workflow;

public sealed class WizardSessionStore
{
    private const int DefaultStepCount = 4;

    private readonly object _syncLock = new();
    private CancellationTokenSource? _currentCancellationTokenSource;

    public WizardSessionStore()
    {
        State = WizardState.CreateInitial(DefaultStepCount);
        GeneratedFiles = Array.Empty<string>();
        InputKind = InputFileKind.Unknown;
    }

    public WizardState State { get; private set; }

    public string? SelectedInputFile { get; private set; }

    public InputFileKind InputKind { get; private set; }

    public string? SelectedOutputFolder { get; private set; }

    public ExtractionResult? ExtractedSubjects { get; private set; }

    public CategorizationResult? LastCategorizationResult { get; private set; }

    public RunSummary? LastRunSummary { get; private set; }

    public IReadOnlyList<string> GeneratedFiles { get; private set; }

    public CancellationTokenSource? CurrentCancellationTokenSource
    {
        get
        {
            lock (_syncLock)
            {
                return _currentCancellationTokenSource;
            }
        }
    }

    public void Reset(int totalStepCount = DefaultStepCount)
    {
        if (totalStepCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalStepCount), "Total step count must be at least 1.");
        }

        lock (_syncLock)
        {
            CancelAndDisposeCurrentOperation();

            SelectedInputFile = null;
            InputKind = InputFileKind.Unknown;
            SelectedOutputFolder = null;
            ExtractedSubjects = null;
            LastCategorizationResult = null;
            LastRunSummary = null;
            GeneratedFiles = Array.Empty<string>();
            State = WizardState.CreateInitial(totalStepCount);
        }
    }

    public void SetCurrentStep(int stepIndex)
    {
        lock (_syncLock)
        {
            EnsureStepIndexIsValid(stepIndex);

            if (State.IsStepLocked(stepIndex))
            {
                throw new InvalidOperationException($"Step {stepIndex} is currently locked.");
            }

            State = State with
            {
                CurrentStepIndex = stepIndex
            };
        }
    }

    public void SetSelectedInputFile(string selectedInputFile, InputFileKind inputKind)
    {
        if (string.IsNullOrWhiteSpace(selectedInputFile))
        {
            throw new ArgumentException("Selected input file cannot be null, empty, or whitespace.", nameof(selectedInputFile));
        }

        if (inputKind == InputFileKind.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(inputKind), "Input file kind must be known.");
        }

        lock (_syncLock)
        {
            SelectedInputFile = selectedInputFile.Trim();
            InputKind = inputKind;
            ExtractedSubjects = null;
            LastCategorizationResult = null;
            LastRunSummary = null;
            GeneratedFiles = Array.Empty<string>();

            State = BuildState(
                currentStepIndex: 0,
                isInputLoaded: true,
                isExtractionReviewComplete: false,
                hasPreview: false,
                isExportComplete: false,
                isBusy: State.IsBusy,
                totalStepCount: State.TotalStepCount);
        }
    }

    public void SetSelectedOutputFolder(string selectedOutputFolder)
    {
        if (string.IsNullOrWhiteSpace(selectedOutputFolder))
        {
            throw new ArgumentException("Selected output folder cannot be null, empty, or whitespace.", nameof(selectedOutputFolder));
        }

        lock (_syncLock)
        {
            SelectedOutputFolder = selectedOutputFolder.Trim();
            ClearExportInternal();
        }
    }

    public void SetExtractionResult(ExtractionResult extractionResult)
    {
        ArgumentNullException.ThrowIfNull(extractionResult);

        lock (_syncLock)
        {
            SelectedInputFile = extractionResult.SourceFile;
            InputKind = extractionResult.InputFileKind;
            ExtractedSubjects = extractionResult;
            LastCategorizationResult = null;
            LastRunSummary = null;
            GeneratedFiles = Array.Empty<string>();

            State = BuildState(
                currentStepIndex: Math.Max(State.CurrentStepIndex, 1),
                isInputLoaded: true,
                isExtractionReviewComplete: false,
                hasPreview: false,
                isExportComplete: false,
                isBusy: State.IsBusy,
                totalStepCount: State.TotalStepCount);
        }
    }

    public void MarkExtractionReviewComplete()
    {
        lock (_syncLock)
        {
            if (ExtractedSubjects is null)
            {
                throw new InvalidOperationException("Extraction review cannot be marked complete before extraction exists.");
            }

            State = BuildState(
                currentStepIndex: Math.Max(State.CurrentStepIndex, 1),
                isInputLoaded: true,
                isExtractionReviewComplete: true,
                hasPreview: State.HasPreview,
                isExportComplete: State.IsExportComplete,
                isBusy: State.IsBusy,
                totalStepCount: State.TotalStepCount);
        }
    }

    public void SetCategorizationResult(CategorizationResult categorizationResult)
    {
        ArgumentNullException.ThrowIfNull(categorizationResult);

        lock (_syncLock)
        {
            if (ExtractedSubjects is null)
            {
                throw new InvalidOperationException("Categorization result cannot be stored before extraction exists.");
            }

            LastCategorizationResult = categorizationResult;
            LastRunSummary = null;
            GeneratedFiles = Array.Empty<string>();

            State = BuildState(
                currentStepIndex: Math.Max(State.CurrentStepIndex, 2),
                isInputLoaded: true,
                isExtractionReviewComplete: true,
                hasPreview: true,
                isExportComplete: false,
                isBusy: State.IsBusy,
                totalStepCount: State.TotalStepCount);
        }
    }

    public void SetRunSummary(RunSummary runSummary, IReadOnlyList<string> generatedFiles)
    {
        ArgumentNullException.ThrowIfNull(runSummary);
        ArgumentNullException.ThrowIfNull(generatedFiles);

        lock (_syncLock)
        {
            if (LastCategorizationResult is null)
            {
                throw new InvalidOperationException("Run summary cannot be stored before categorization exists.");
            }

            LastRunSummary = runSummary;
            GeneratedFiles = generatedFiles
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .ToArray();

            State = BuildState(
                currentStepIndex: Math.Max(State.CurrentStepIndex, 3),
                isInputLoaded: true,
                isExtractionReviewComplete: true,
                hasPreview: true,
                isExportComplete: true,
                isBusy: State.IsBusy,
                totalStepCount: State.TotalStepCount);
        }
    }

    public void ClearPreviewAndExport()
    {
        lock (_syncLock)
        {
            LastCategorizationResult = null;
            LastRunSummary = null;
            GeneratedFiles = Array.Empty<string>();

            State = BuildState(
                currentStepIndex: State.CurrentStepIndex,
                isInputLoaded: ExtractedSubjects is not null || !string.IsNullOrWhiteSpace(SelectedInputFile),
                isExtractionReviewComplete: ExtractedSubjects is not null && State.IsExtractionReviewComplete,
                hasPreview: false,
                isExportComplete: false,
                isBusy: State.IsBusy,
                totalStepCount: State.TotalStepCount);
        }
    }

    public void ClearExport()
    {
        lock (_syncLock)
        {
            ClearExportInternal();
        }
    }

    public void BeginBusyOperation(CancellationTokenSource cancellationTokenSource)
    {
        ArgumentNullException.ThrowIfNull(cancellationTokenSource);

        lock (_syncLock)
        {
            if (State.IsBusy)
            {
                throw new InvalidOperationException("Another busy operation is already active.");
            }

            _currentCancellationTokenSource = cancellationTokenSource;

            State = State with
            {
                IsBusy = true
            };
        }
    }

    public void CancelBusyOperation()
    {
        lock (_syncLock)
        {
            _currentCancellationTokenSource?.Cancel();
        }
    }

    public void CompleteBusyOperation()
    {
        lock (_syncLock)
        {
            CancelAndDisposeCurrentOperation();

            State = State with
            {
                IsBusy = false
            };
        }
    }

    private void ClearExportInternal()
    {
        LastRunSummary = null;
        GeneratedFiles = Array.Empty<string>();

        State = BuildState(
            currentStepIndex: State.CurrentStepIndex,
            isInputLoaded: State.IsInputLoaded,
            isExtractionReviewComplete: State.IsExtractionReviewComplete,
            hasPreview: State.HasPreview,
            isExportComplete: false,
            isBusy: State.IsBusy,
            totalStepCount: State.TotalStepCount);
    }

    private void CancelAndDisposeCurrentOperation()
    {
        try
        {
            _currentCancellationTokenSource?.Cancel();
        }
        finally
        {
            _currentCancellationTokenSource?.Dispose();
            _currentCancellationTokenSource = null;
        }
    }

    private WizardState BuildState(
        int currentStepIndex,
        bool isInputLoaded,
        bool isExtractionReviewComplete,
        bool hasPreview,
        bool isExportComplete,
        bool isBusy,
        int totalStepCount)
    {
        var lockedStepIndices = new List<int>();

        if (!isInputLoaded)
        {
            lockedStepIndices.AddRange(Enumerable.Range(1, Math.Max(0, totalStepCount - 1)));
        }
        else if (!isExtractionReviewComplete)
        {
            if (totalStepCount > 2)
            {
                lockedStepIndices.AddRange(Enumerable.Range(2, totalStepCount - 2));
            }
        }
        else if (!hasPreview)
        {
            if (totalStepCount > 3)
            {
                lockedStepIndices.AddRange(Enumerable.Range(3, totalStepCount - 3));
            }
        }

        var adjustedStepIndex = currentStepIndex;

        while (adjustedStepIndex > 0 && lockedStepIndices.Contains(adjustedStepIndex))
        {
            adjustedStepIndex--;
        }

        return new WizardState
        {
            TotalStepCount = totalStepCount,
            CurrentStepIndex = adjustedStepIndex,
            LockedStepIndices = lockedStepIndices.ToArray(),
            IsInputLoaded = isInputLoaded,
            IsExtractionReviewComplete = isExtractionReviewComplete,
            HasPreview = hasPreview,
            IsExportComplete = isExportComplete,
            IsBusy = isBusy
        };
    }

    private void EnsureStepIndexIsValid(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= State.TotalStepCount)
        {
            throw new ArgumentOutOfRangeException(nameof(stepIndex), "Step index is outside the valid range.");
        }
    }
}
