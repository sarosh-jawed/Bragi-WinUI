using Bragi.Application.Workflow;
using Bragi.Domain.Enums;
using Bragi.Domain.Models;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;

namespace Bragi.Tests.Workflow;

public sealed class WizardSessionStoreTests
{
    [Fact]
    public void InitialState_LocksLaterSteps_AndKeepsStartLinear()
    {
        var store = new WizardSessionStore();

        Assert.Equal(0, store.State.CurrentStepIndex);
        Assert.False(store.State.IsStepLocked(0));
        Assert.False(store.State.IsStepLocked(1));
        Assert.True(store.State.IsStepLocked(2));
        Assert.True(store.State.IsStepLocked(3));
        Assert.True(store.State.IsStepLocked(4));
    }

    [Fact]
    public void StateSurvivesNavigation_AndPreviewExportClear_WhenInputChanges()
    {
        var store = new WizardSessionStore();

        var extractionResult = CreateExtractionResult("C:\\Input\\first.txt", InputFileKind.PlainText);
        var categorizationResult = CreateCategorizationResult();
        var runSummary = CreateRunSummary("C:\\Input\\first.txt", InputFileKind.PlainText);

        store.SetExtractionResult(extractionResult);
        store.MarkExtractionReviewComplete();
        store.SetCategorizationResult(categorizationResult);
        store.SetRunSummary(runSummary, ["C:\\Output\\ArtSubjects.txt", "C:\\Output\\RunSummary.txt"]);
        store.SetCurrentStep(4);

        Assert.Equal(4, store.State.CurrentStepIndex);
        Assert.NotNull(store.ExtractedSubjects);
        Assert.NotNull(store.LastCategorizationResult);
        Assert.NotNull(store.LastRunSummary);
        Assert.Equal(2, store.GeneratedFiles.Count);

        store.SetSelectedInputFile("C:\\Input\\second.csv", InputFileKind.Csv);

        Assert.Equal("C:\\Input\\second.csv", store.SelectedInputFile);
        Assert.Equal(InputFileKind.Csv, store.InputKind);
        Assert.Null(store.ExtractedSubjects);
        Assert.Null(store.LastCategorizationResult);
        Assert.Null(store.LastRunSummary);
        Assert.Empty(store.GeneratedFiles);

        Assert.Equal(1, store.State.CurrentStepIndex);
        Assert.False(store.State.IsInputLoaded);
        Assert.False(store.State.IsExtractionReviewComplete);
        Assert.False(store.State.HasPreview);
        Assert.False(store.State.IsExportComplete);
        Assert.False(store.State.IsStepLocked(1));
        Assert.True(store.State.IsStepLocked(2));
        Assert.True(store.State.IsStepLocked(3));
        Assert.True(store.State.IsStepLocked(4));
    }

    [Fact]
    public void StepNavigation_DoesNotLoseStoredPreviewState()
    {
        var store = new WizardSessionStore();

        store.SetExtractionResult(CreateExtractionResult("C:\\Input\\subjects.txt", InputFileKind.PlainText));
        store.MarkExtractionReviewComplete();
        store.SetCategorizationResult(CreateCategorizationResult());

        store.SetCurrentStep(2);
        Assert.NotNull(store.ExtractedSubjects);
        Assert.NotNull(store.LastCategorizationResult);
        Assert.True(store.State.HasPreview);

        store.SetCurrentStep(3);
        Assert.NotNull(store.ExtractedSubjects);
        Assert.NotNull(store.LastCategorizationResult);
        Assert.True(store.State.HasPreview);
    }

    [Fact]
    public void BusyOperations_CanBeCancelledCleanly()
    {
        var store = new WizardSessionStore();
        using var cancellationTokenSource = new CancellationTokenSource();

        store.BeginBusyOperation(cancellationTokenSource);

        Assert.True(store.State.IsBusy);
        Assert.NotNull(store.CurrentCancellationTokenSource);

        store.CancelBusyOperation();

        Assert.True(cancellationTokenSource.IsCancellationRequested);

        store.CompleteBusyOperation();

        Assert.False(store.State.IsBusy);
        Assert.Null(store.CurrentCancellationTokenSource);
    }

    private static ExtractionResult CreateExtractionResult(string sourceFile, InputFileKind inputFileKind)
    {
        var extractedSubject = new ExtractedSubject(
            new SubjectEntry(
                new SubjectText("Art"),
                new NormalizedSubjectText("art"),
                sourceFile,
                1,
                null,
                null,
                inputFileKind),
            1);

        return new ExtractionResult(
            sourceFile,
            inputFileKind,
            [extractedSubject],
            totalRecordsRead: 1,
            blankOrIgnoredCount: 0,
            duplicateCount: 0,
            parseWarningCount: 0);
    }

    private static CategorizationResult CreateCategorizationResult()
    {
        var extractedSubject = new ExtractedSubject(
            new SubjectEntry(
                new SubjectText("Art"),
                new NormalizedSubjectText("art"),
                "C:\\Input\\first.txt",
                1,
                null,
                null,
                InputFileKind.PlainText),
            1);

        return new CategorizationResult(
            [
                new CategorizedSubject(
                    extractedSubject,
                    [
                        new CategoryMatch(
                            new CategoryKey("art"),
                            new OutputFileName("ArtSubjects.txt"),
                            "Matched include keyword: art")
                    ])
            ],
            [],
            new Dictionary<CategoryKey, int>
            {
                [new CategoryKey("art")] = 1
            });
    }

    private static RunSummary CreateRunSummary(string sourceFile, InputFileKind inputFileKind)
    {
        return new RunSummary(
            sourceFile,
            inputFileKind,
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 1, 12, 1, 0, TimeSpan.Zero),
            totalRecordsRead: 1,
            extractedSubjectCount: 1,
            categorizedAssignmentCount: 1,
            uncategorizedSubjectCount: 0,
            blankOrIgnoredCount: 0,
            duplicateCount: 0,
            parseWarningCount: 0,
            new Dictionary<CategoryKey, int>
            {
                [new CategoryKey("art")] = 1
            });
    }
}
