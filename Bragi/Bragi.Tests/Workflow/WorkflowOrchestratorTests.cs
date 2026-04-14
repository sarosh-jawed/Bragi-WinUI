using Bragi.Application.Configuration;
using Bragi.Application.Workflow;
using Bragi.Domain.Enums;
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

            var config = new BragiConfig
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
                    RootPath = tempRoot,
                    UncategorizedFileName = "NotCategorizedSubjects.txt",
                    RunSummaryFileName = "RunSummary.txt",
                    CreateMonthlySubfolder = false
                },
                TextTemplate = new TextTemplate(),
                BehaviorOptions = new BehaviorOptions()
            };

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
                new RunSummaryBuilder(),
                config);

            var workflowOrchestrator = new WorkflowOrchestrator(
                NullLogger<WorkflowOrchestrator>.Instance,
                config,
                wizardSessionStore,
                inputIngestService,
                subjectExtractionService,
                categorizationService,
                textExportService);

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
}
