using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Application.Workflow;
using Bragi.Domain.Enums;
using Bragi.Domain.Models;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;
using Bragi.Infrastructure.Categorization;
using Bragi.Infrastructure.Extraction;
using Bragi.Infrastructure.Export;
using Bragi.Infrastructure.Ingestion;
using Bragi.Infrastructure.Workflow;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bragi.Tests.Workflow;

public sealed class WorkflowOrchestratorTests
{
    [Fact]
    public async Task ExecuteAsync_PopulatesSessionState_AndGeneratedFiles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"bragi-workflow-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        var inputFilePath = Path.Combine(tempRoot, "subjects.txt");

        try
        {
            await File.WriteAllTextAsync(
                inputFilePath,
                "Art\r\nUnknown topic\r\n");

            var config = CreateConfig(tempRoot);

            var wizardSessionStore = new WizardSessionStore();
            var inputIngestService = new InputIngestService(NullLogger<InputIngestService>.Instance);
            var subjectExtractionService = new SubjectExtractionService(NullLogger<SubjectExtractionService>.Instance);

            var subjectNormalizationHelper = new SubjectNormalizationHelper();
            var keywordMatcher = new KeywordMatcher(subjectNormalizationHelper);
            var exclusionMatcher = new ExclusionMatcher(keywordMatcher);

            var categorizationService = new CategorizationService(
                NullLogger<CategorizationService>.Instance,
                subjectNormalizationHelper,
                keywordMatcher,
                exclusionMatcher);

            var textExportService = new TextExportService(
                NullLogger<TextExportService>.Instance,
                new TextBodyBuilder(),
                config);

            var workflowOrchestrator = new WorkflowOrchestrator(
                NullLogger<WorkflowOrchestrator>.Instance,
                config,
                wizardSessionStore,
                inputIngestService,
                subjectExtractionService,
                categorizationService,
                textExportService,
                new RunSummaryBuilder());

            var runSummary = await workflowOrchestrator.ExecuteAsync(inputFilePath);

            Assert.NotNull(wizardSessionStore.ExtractedSubjects);
            Assert.NotNull(wizardSessionStore.LastCategorizationResult);
            Assert.NotNull(wizardSessionStore.LastRunSummary);
            Assert.Equal(runSummary.SourceFile, wizardSessionStore.LastRunSummary!.SourceFile);

            Assert.True(wizardSessionStore.State.IsInputLoaded);
            Assert.True(wizardSessionStore.State.IsExtractionReviewComplete);
            Assert.True(wizardSessionStore.State.HasPreview);
            Assert.True(wizardSessionStore.State.IsExportComplete);
            Assert.False(wizardSessionStore.State.IsBusy);

            Assert.Equal(3, wizardSessionStore.GeneratedFiles.Count);
            Assert.All(wizardSessionStore.GeneratedFiles, generatedFile =>
            {
                Assert.True(File.Exists(generatedFile), $"Expected generated file to exist: {generatedFile}");
            });

            var artFilePath = Path.Combine(tempRoot, "ArtSubjects.txt");
            var uncategorizedFilePath = Path.Combine(tempRoot, "NotCategorizedSubjects.txt");
            var runSummaryFilePath = Path.Combine(tempRoot, "RunSummary.txt");

            Assert.Contains(artFilePath, wizardSessionStore.GeneratedFiles);
            Assert.Contains(uncategorizedFilePath, wizardSessionStore.GeneratedFiles);
            Assert.Contains(runSummaryFilePath, wizardSessionStore.GeneratedFiles);

            var artFileContent = await File.ReadAllTextAsync(artFilePath);
            var uncategorizedFileContent = await File.ReadAllTextAsync(uncategorizedFilePath);

            Assert.Equal("Art", artFileContent);
            Assert.Equal("Unknown topic", uncategorizedFileContent);
            Assert.Equal(InputFileKind.PlainText, runSummary.InputFileKind);
            Assert.Equal(2, runSummary.ExtractedSubjectCount);
            Assert.Equal(1, runSummary.CategorizedAssignmentCount);
            Assert.Equal(1, runSummary.UncategorizedSubjectCount);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PreviewExtractionAsync_WhenCancelled_LeavesSessionStable()
    {
        var config = CreateConfig(Path.GetTempPath());
        var wizardSessionStore = new WizardSessionStore();

        var workflowOrchestrator = new WorkflowOrchestrator(
            NullLogger<WorkflowOrchestrator>.Instance,
            config,
            wizardSessionStore,
            new CancellingInputIngestService(cancelDuringTextRead: true),
            new StubSubjectExtractionService(CreateExtractionResult("C:\\Input\\subjects.txt", InputFileKind.PlainText)),
            new StubCategorizationService(CreateCategorizationResult("C:\\Input\\subjects.txt", InputFileKind.PlainText)),
            new NoOpTextExportService(),
            new RunSummaryBuilder());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            workflowOrchestrator.PreviewExtractionAsync("C:\\Input\\subjects.txt"));

        Assert.False(wizardSessionStore.State.IsBusy);
        Assert.False(wizardSessionStore.State.IsInputLoaded);
        Assert.Null(wizardSessionStore.ExtractedSubjects);
        Assert.Null(wizardSessionStore.LastCategorizationResult);
        Assert.Null(wizardSessionStore.LastRunSummary);
    }

    [Fact]
    public async Task PreviewCategorizationAsync_WhenCancelled_LeavesPreviewStateStable()
    {
        var config = CreateConfig(Path.GetTempPath());
        var wizardSessionStore = new WizardSessionStore();

        var extractionResult = CreateExtractionResult("C:\\Input\\subjects.txt", InputFileKind.PlainText);

        var workflowOrchestrator = new WorkflowOrchestrator(
            NullLogger<WorkflowOrchestrator>.Instance,
            config,
            wizardSessionStore,
            new SuccessfulInputIngestService(InputFileKind.PlainText, "Art"),
            new StubSubjectExtractionService(extractionResult),
            new CancellingCategorizationService(),
            new NoOpTextExportService(),
            new RunSummaryBuilder());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            workflowOrchestrator.PreviewCategorizationAsync("C:\\Input\\subjects.txt"));

        Assert.False(wizardSessionStore.State.IsBusy);
        Assert.True(wizardSessionStore.State.IsInputLoaded);
        Assert.True(wizardSessionStore.State.IsExtractionReviewComplete);
        Assert.False(wizardSessionStore.State.HasPreview);
        Assert.False(wizardSessionStore.State.IsExportComplete);

        Assert.NotNull(wizardSessionStore.ExtractedSubjects);
        Assert.Null(wizardSessionStore.LastCategorizationResult);
        Assert.Null(wizardSessionStore.LastRunSummary);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledDuringExport_LeavesExportStateStable()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"bragi-workflow-cancel-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = CreateConfig(tempRoot);
            var wizardSessionStore = new WizardSessionStore();

            var extractionResult = CreateExtractionResult("C:\\Input\\subjects.txt", InputFileKind.PlainText);
            var categorizationResult = CreateCategorizationResult("C:\\Input\\subjects.txt", InputFileKind.PlainText);

            var workflowOrchestrator = new WorkflowOrchestrator(
                NullLogger<WorkflowOrchestrator>.Instance,
                config,
                wizardSessionStore,
                new SuccessfulInputIngestService(InputFileKind.PlainText, "Art"),
                new StubSubjectExtractionService(extractionResult),
                new StubCategorizationService(categorizationResult),
                new CancellingTextExportService(),
                new RunSummaryBuilder());

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                workflowOrchestrator.ExecuteAsync("C:\\Input\\subjects.txt"));

            Assert.False(wizardSessionStore.State.IsBusy);
            Assert.True(wizardSessionStore.State.IsInputLoaded);
            Assert.True(wizardSessionStore.State.IsExtractionReviewComplete);
            Assert.True(wizardSessionStore.State.HasPreview);
            Assert.False(wizardSessionStore.State.IsExportComplete);

            Assert.NotNull(wizardSessionStore.ExtractedSubjects);
            Assert.NotNull(wizardSessionStore.LastCategorizationResult);
            Assert.Null(wizardSessionStore.LastRunSummary);
            Assert.Empty(wizardSessionStore.GeneratedFiles);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static BragiConfig CreateConfig(string outputRoot)
    {
        return new BragiConfig
        {
            CsvColumns = new CsvColumns(),
            InputOptions = new InputOptions(),
            CategoryRules =
            [
                new CategoryRule
                {
                    Key = "art",
                    DisplayName = "Art",
                    OutputFileName = "ArtSubjects.txt",
                    IncludeKeywords = ["art"],
                    ExcludeKeywords = [],
                    RequireAnyKeywords = [],
                    DisableForFiction = false,
                    DisableForJuvenile = false,
                    IncludeMatchMode = CategoryMatchMode.Contains,
                    ExcludeMatchMode = CategoryMatchMode.Contains,
                    RequireAnyMatchMode = CategoryMatchMode.Contains,
                    SortOrder = 10,
                    Enabled = true
                }
            ],
            Output = new Output
            {
                RootPath = outputRoot,
                UncategorizedFileName = "NotCategorizedSubjects.txt",
                RunSummaryFileName = "RunSummary.txt",
                CreateMonthlySubfolder = false
            },
            TextTemplate = new TextTemplate(),
            BehaviorOptions = new BehaviorOptions()
        };
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

    private static CategorizationResult CreateCategorizationResult(string sourceFile, InputFileKind inputFileKind)
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

    private sealed class SuccessfulInputIngestService : IInputIngestService
    {
        private readonly InputFileKind _inputFileKind;
        private readonly string _textContent;

        public SuccessfulInputIngestService(InputFileKind inputFileKind, string textContent)
        {
            _inputFileKind = inputFileKind;
            _textContent = textContent;
        }

        public Task<InputFileKind> DetectInputFileKindAsync(
            string sourceFilePath,
            InputOptions inputOptions,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_inputFileKind);
        }

        public Task<string> ReadAllTextAsync(
            string sourceFilePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_textContent);
        }

        public Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadCsvRowsAsync(
            string sourceFilePath,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IReadOnlyDictionary<string, string?>> rows = [];
            return Task.FromResult(rows);
        }
    }

    private sealed class CancellingInputIngestService : IInputIngestService
    {
        private readonly bool _cancelDuringTextRead;

        public CancellingInputIngestService(bool cancelDuringTextRead)
        {
            _cancelDuringTextRead = cancelDuringTextRead;
        }

        public Task<InputFileKind> DetectInputFileKindAsync(
            string sourceFilePath,
            InputOptions inputOptions,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InputFileKind.PlainText);
        }

        public Task<string> ReadAllTextAsync(
            string sourceFilePath,
            CancellationToken cancellationToken = default)
        {
            if (_cancelDuringTextRead)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Task.FromResult(string.Empty);
        }

        public Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadCsvRowsAsync(
            string sourceFilePath,
            CancellationToken cancellationToken = default)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class StubSubjectExtractionService : ISubjectExtractionService
    {
        private readonly ExtractionResult _extractionResult;

        public StubSubjectExtractionService(ExtractionResult extractionResult)
        {
            _extractionResult = extractionResult;
        }

        public Task<ExtractionResult> ExtractFromPlainTextAsync(
            string sourceFilePath,
            string textContent,
            InputOptions inputOptions,
            BehaviorOptions behaviorOptions,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_extractionResult);
        }

        public Task<ExtractionResult> ExtractFromCsvAsync(
            string sourceFilePath,
            IReadOnlyList<IReadOnlyDictionary<string, string?>> rows,
            CsvColumns csvColumns,
            InputOptions inputOptions,
            BehaviorOptions behaviorOptions,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_extractionResult);
        }
    }

    private sealed class StubCategorizationService : ICategorizationService
    {
        private readonly CategorizationResult _categorizationResult;

        public StubCategorizationService(CategorizationResult categorizationResult)
        {
            _categorizationResult = categorizationResult;
        }

        public Task<CategorizationResult> CategorizeAsync(
            ExtractionResult extractionResult,
            IReadOnlyList<CategoryRule> categoryRules,
            BehaviorOptions behaviorOptions,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_categorizationResult);
        }
    }

    private sealed class CancellingCategorizationService : ICategorizationService
    {
        public Task<CategorizationResult> CategorizeAsync(
            ExtractionResult extractionResult,
            IReadOnlyList<CategoryRule> categoryRules,
            BehaviorOptions behaviorOptions,
            CancellationToken cancellationToken = default)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class NoOpTextExportService : ITextExportService
    {
        public Task<ExportResult> ExportAsync(
            CategorizationResult categorizationResult,
            DateTimeOffset exportTimestampUtc,
            Output outputOptions,
            TextTemplate textTemplate,
            IReadOnlyList<CategoryRule> categoryRules,
            CancellationToken cancellationToken = default)
        {
            var outputDirectory = outputOptions.CreateMonthlySubfolder
                ? Path.Combine(outputOptions.RootPath, exportTimestampUtc.ToString(outputOptions.MonthlySubfolderFormat))
                : outputOptions.RootPath;

            return Task.FromResult(new ExportResult(
                outputDirectory,
                Array.Empty<string>(),
                new Dictionary<CategoryKey, int>(),
                0,
                0,
                outputsSorted: false,
                outputsDeduplicated: false));
        }
    }

    private sealed class CancellingTextExportService : ITextExportService
    {
        public Task<ExportResult> ExportAsync(
            CategorizationResult categorizationResult,
            DateTimeOffset exportTimestampUtc,
            Output outputOptions,
            TextTemplate textTemplate,
            IReadOnlyList<CategoryRule> categoryRules,
            CancellationToken cancellationToken = default)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }
}
