using System.Text;
using Bragi.Application.Configuration;
using Bragi.Domain.Enums;
using Bragi.Domain.Models;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;
using Bragi.Infrastructure.Export;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bragi.Tests.Export;

public sealed class TextExportServiceTests
{
    [Fact]
    public async Task ExportAsync_GeneratesDeterministicFiles_AndWritesUtf8WithoutBom()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"bragi-export-{Guid.NewGuid():N}");

        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = new BragiConfig
            {
                BehaviorOptions = new BehaviorOptions
                {
                    SortOutputs = true,
                    DeduplicateOutputs = true
                }
            };

            var service = new TextExportService(
                NullLogger<TextExportService>.Instance,
                new TextBodyBuilder(),
                new RunSummaryBuilder(),
                config);

            var categoryRules = new[]
            {
                CreateRule("art", "ArtSubjects.txt", sortOrder: 10),
                CreateRule("business", "BusinessSubjects.txt", sortOrder: 20)
            };

            var categorizationResult = new CategorizationResult(
                categorizedSubjects:
                [
                    new CategorizedSubject(
                        CreateExtractedSubject(2, "Art", 3),
                        [new CategoryMatch(new CategoryKey("art"), new OutputFileName("ArtSubjects.txt"), "Matched include keyword: art")]),
                    new CategorizedSubject(
                        CreateExtractedSubject(1, "Art", 2),
                        [new CategoryMatch(new CategoryKey("art"), new OutputFileName("ArtSubjects.txt"), "Matched include keyword: art")])
                ],
                uncategorizedSubjects:
                [
                    new UncategorizedSubject(
                        CreateExtractedSubject(3, "Unknown topic", 4),
                        "No configured category matched.")
                ],
                categoryCounts: new Dictionary<CategoryKey, int>
                {
                    [new CategoryKey("art")] = 2,
                    [new CategoryKey("business")] = 0
                });

            var runSummary = new RunSummary(
                sourceFile: @"C:\Input\subjects.csv",
                inputFileKind: InputFileKind.Csv,
                runStartedAtUtc: new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
                runCompletedAtUtc: new DateTimeOffset(2026, 4, 1, 12, 5, 0, TimeSpan.Zero),
                totalRecordsRead: 3,
                extractedSubjectCount: 3,
                categorizedAssignmentCount: 2,
                uncategorizedSubjectCount: 1,
                blankOrIgnoredCount: 0,
                duplicateCount: 1,
                parseWarningCount: 0,
                categoryCounts: new Dictionary<CategoryKey, int>
                {
                    [new CategoryKey("art")] = 2,
                    [new CategoryKey("business")] = 0
                });

            var outputOptions = new Output
            {
                RootPath = tempRoot,
                UncategorizedFileName = "NotCategorizedSubjects.txt",
                RunSummaryFileName = "RunSummary.txt",
                CreateMonthlySubfolder = false
            };

            var textTemplate = new TextTemplate();

            await service.ExportAsync(
                categorizationResult,
                runSummary,
                outputOptions,
                textTemplate,
                categoryRules);

            var artFilePath = Path.Combine(tempRoot, "ArtSubjects.txt");
            var businessFilePath = Path.Combine(tempRoot, "BusinessSubjects.txt");
            var uncategorizedFilePath = Path.Combine(tempRoot, "NotCategorizedSubjects.txt");
            var runSummaryFilePath = Path.Combine(tempRoot, "RunSummary.txt");

            Assert.True(File.Exists(artFilePath));
            Assert.True(File.Exists(businessFilePath));
            Assert.True(File.Exists(uncategorizedFilePath));
            Assert.True(File.Exists(runSummaryFilePath));

            var artFileContent = await File.ReadAllTextAsync(artFilePath);
            var businessFileContent = await File.ReadAllTextAsync(businessFilePath);
            var uncategorizedFileContent = await File.ReadAllTextAsync(uncategorizedFilePath);
            var runSummaryContent = await File.ReadAllTextAsync(runSummaryFilePath);

            Assert.Equal("Art", artFileContent);
            Assert.Equal(string.Empty, businessFileContent);
            Assert.Equal("Unknown topic", uncategorizedFileContent);

            Assert.Contains("Input file name: subjects.csv", runSummaryContent);
            Assert.Contains("Total extracted subjects: 3", runSummaryContent);
            Assert.Contains("Total categorized assignments: 2", runSummaryContent);
            Assert.Contains("Total uncategorized subjects: 1", runSummaryContent);
            Assert.Contains("Blank/ignored count: 0", runSummaryContent);
            Assert.Contains("Parse warning count: 0", runSummaryContent);
            Assert.Contains("Duplicate count: 1", runSummaryContent);
            Assert.Contains("art: 2", runSummaryContent);
            Assert.Contains("business: 0", runSummaryContent);

            var artFileBytes = await File.ReadAllBytesAsync(artFilePath);
            var utf8Bom = Encoding.UTF8.GetPreamble();

            Assert.False(
                artFileBytes.Take(utf8Bom.Length).SequenceEqual(utf8Bom),
                "ArtSubjects.txt should be written as UTF-8 without BOM.");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static CategoryRule CreateRule(
        string key,
        string outputFileName,
        int sortOrder)
    {
        return new CategoryRule
        {
            Key = key,
            DisplayName = key,
            OutputFileName = outputFileName,
            IncludeKeywords = [key],
            ExcludeKeywords = [],
            RequireAnyKeywords = [],
            DisableForFiction = false,
            DisableForJuvenile = false,
            IncludeMatchMode = CategoryMatchMode.Contains,
            ExcludeMatchMode = CategoryMatchMode.Contains,
            RequireAnyMatchMode = CategoryMatchMode.Contains,
            SortOrder = sortOrder,
            Enabled = true
        };
    }

    private static ExtractedSubject CreateExtractedSubject(
        int sequenceNumber,
        string originalSubject,
        int sourceRowNumber)
    {
        var subjectEntry = new SubjectEntry(
            new SubjectText(originalSubject),
            new NormalizedSubjectText(originalSubject.ToLowerInvariant()),
            "test-input.csv",
            sourceRowNumber,
            null,
            null,
            InputFileKind.Csv);

        return new ExtractedSubject(subjectEntry, sequenceNumber);
    }
}
