using Bragi.Application.Configuration;
using Bragi.Domain.Enums;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;
using Bragi.Infrastructure.Export;

namespace Bragi.Tests.Export;

public sealed class RunSummaryBuilderTests
{
    [Fact]
    public void Build_RendersAssignmentCounts_ExportCounts_AndExplanationNote()
    {
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
            },
            exportedCategoryLineCounts: new Dictionary<CategoryKey, int>
            {
                [new CategoryKey("art")] = 1,
                [new CategoryKey("business")] = 0
            },
            exportedUncategorizedLineCount: 1,
            exportedCategoryLineCountTotal: 1,
            outputsSorted: true,
            outputsDeduplicated: true);

        var categoryRules = new[]
        {
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
            },
            new CategoryRule
            {
                Key = "business",
                DisplayName = "Business",
                OutputFileName = "BusinessSubjects.txt",
                IncludeKeywords = ["business"],
                ExcludeKeywords = [],
                RequireAnyKeywords = [],
                DisableForFiction = false,
                DisableForJuvenile = false,
                IncludeMatchMode = CategoryMatchMode.Contains,
                ExcludeMatchMode = CategoryMatchMode.Contains,
                RequireAnyMatchMode = CategoryMatchMode.Contains,
                SortOrder = 20,
                Enabled = true
            }
        };

        var builder = new RunSummaryBuilder();
        var text = builder.Build(runSummary, new TextTemplate(), categoryRules);

        Assert.Contains("Total categorized assignments: 2", text);
        Assert.Contains("Exported categorized line total: 1", text);
        Assert.Contains("Exported uncategorized line total: 1", text);
        Assert.Contains("Outputs sorted: true", text);
        Assert.Contains("Outputs deduplicated: true", text);
        Assert.Contains("Note: category assignment counts may be higher than exported file line counts because export output is sorted and deduplicated.", text);
        Assert.Contains("art: 2", text);
        Assert.Contains("art: 1", text);
        Assert.Contains("business: 0", text);
    }
}
