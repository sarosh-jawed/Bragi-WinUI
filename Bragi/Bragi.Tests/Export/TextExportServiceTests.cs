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
    public async Task ExportAsync_GeneratesDeterministicFiles_AndReturnsFinalExportFacts()
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

            var outputOptions = new Output
            {
                RootPath = tempRoot,
                UncategorizedFileName = "NotCategorizedSubjects.txt",
                RunSummaryFileName = "RunSummary.txt",
                CreateMonthlySubfolder = false
            };

            var textTemplate = new TextTemplate();

            var exportResult = await service.ExportAsync(
                categorizationResult,
                new DateTimeOffset(2026, 4, 1, 12, 5, 0, TimeSpan.Zero),
                outputOptions,
                textTemplate,
                categoryRules);

            var artFilePath = Path.Combine(tempRoot, "ArtSubjects.txt");
            var businessFilePath = Path.Combine(tempRoot, "BusinessSubjects.txt");
            var uncategorizedFilePath = Path.Combine(tempRoot, "NotCategorizedSubjects.txt");

            Assert.True(File.Exists(artFilePath));
            Assert.True(File.Exists(businessFilePath));
            Assert.True(File.Exists(uncategorizedFilePath));

            var artFileContent = await File.ReadAllTextAsync(artFilePath);
            var businessFileContent = await File.ReadAllTextAsync(businessFilePath);
            var uncategorizedFileContent = await File.ReadAllTextAsync(uncategorizedFilePath);

            Assert.Equal("Art", artFileContent);
            Assert.Equal(string.Empty, businessFileContent);
            Assert.Equal("Unknown topic", uncategorizedFileContent);

            Assert.Equal(tempRoot, exportResult.OutputDirectory);
            Assert.Equal(3, exportResult.GeneratedFiles.Count);
            Assert.Contains(artFilePath, exportResult.GeneratedFiles);
            Assert.Contains(businessFilePath, exportResult.GeneratedFiles);
            Assert.Contains(uncategorizedFilePath, exportResult.GeneratedFiles);

            Assert.True(exportResult.CategoryExportLineCounts.TryGetValue(new CategoryKey("art"), out var artLineCount));
            Assert.Equal(1, artLineCount);

            Assert.True(exportResult.CategoryExportLineCounts.TryGetValue(new CategoryKey("business"), out var businessLineCount));
            Assert.Equal(0, businessLineCount);

            Assert.Equal(1, exportResult.UncategorizedExportLineCount);
            Assert.Equal(1, exportResult.TotalExportedCategoryLines);
            Assert.True(exportResult.OutputsSorted);
            Assert.True(exportResult.OutputsDeduplicated);

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
