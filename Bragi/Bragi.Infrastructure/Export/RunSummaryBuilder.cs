using System.Text;
using Bragi.Application.Configuration;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;

namespace Bragi.Infrastructure.Export;

public sealed class RunSummaryBuilder
{
    public string Build(
        RunSummary runSummary,
        TextTemplate textTemplate,
        IReadOnlyList<CategoryRule> categoryRules)
    {
        ArgumentNullException.ThrowIfNull(runSummary);
        ArgumentNullException.ThrowIfNull(textTemplate);
        ArgumentNullException.ThrowIfNull(categoryRules);

        var orderedRules = categoryRules
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var builder = new StringBuilder();

        builder.AppendLine(textTemplate.RunSummaryHeaderTemplate);
        builder.AppendLine();

        builder.AppendLine(RenderDetailLine(textTemplate, "Input file name", Path.GetFileName(runSummary.SourceFile)));
        builder.AppendLine(RenderDetailLine(textTemplate, "Input file path", runSummary.SourceFile));
        builder.AppendLine(RenderDetailLine(textTemplate, "Input file kind", runSummary.InputFileKind.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Run started (UTC)", runSummary.RunStartedAtUtc.ToString("u")));
        builder.AppendLine(RenderDetailLine(textTemplate, "Run completed (UTC)", runSummary.RunCompletedAtUtc.ToString("u")));
        builder.AppendLine(RenderDetailLine(textTemplate, "Duration", runSummary.Duration.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Total records read", runSummary.TotalRecordsRead.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Total extracted subjects", runSummary.ExtractedSubjectCount.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Total categorized assignments", runSummary.CategorizedAssignmentCount.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Total uncategorized subjects", runSummary.UncategorizedSubjectCount.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Blank/ignored count", runSummary.BlankOrIgnoredCount.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Parse warning count", runSummary.ParseWarningCount.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Duplicate count", runSummary.DuplicateCount.ToString()));
        builder.AppendLine();

        builder.AppendLine("Category assignment counts");

        foreach (var rule in orderedRules)
        {
            var categoryKey = new CategoryKey(rule.Key);
            var categoryCount = runSummary.CategoryCounts.TryGetValue(categoryKey, out var count)
                ? count
                : 0;

            builder.AppendLine(RenderCategoryLine(
                textTemplate,
                rule.Key,
                rule.DisplayName,
                rule.OutputFileName,
                categoryCount));
        }

        builder.AppendLine();
        builder.AppendLine("Exported file counts");
        builder.AppendLine(RenderDetailLine(textTemplate, "Outputs sorted", runSummary.OutputsSorted.ToString().ToLowerInvariant()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Outputs deduplicated", runSummary.OutputsDeduplicated.ToString().ToLowerInvariant()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Exported categorized line total", runSummary.ExportedCategoryLineCountTotal.ToString()));
        builder.AppendLine(RenderDetailLine(textTemplate, "Exported uncategorized line total", runSummary.ExportedUncategorizedLineCount.ToString()));
        builder.AppendLine("Note: category assignment counts may be higher than exported file line counts because export output is sorted and deduplicated.");
        builder.AppendLine();
        builder.AppendLine("Per-category exported unique line counts");

        foreach (var rule in orderedRules)
        {
            var categoryKey = new CategoryKey(rule.Key);
            var exportedLineCount = runSummary.ExportedCategoryLineCounts.TryGetValue(categoryKey, out var count)
                ? count
                : 0;

            builder.AppendLine(RenderCategoryLine(
                textTemplate,
                rule.Key,
                rule.DisplayName,
                rule.OutputFileName,
                exportedLineCount));
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderDetailLine(
        TextTemplate textTemplate,
        string label,
        string value)
    {
        return textTemplate.RunSummaryDetailLineTemplate
            .Replace("{{Label}}", label, StringComparison.Ordinal)
            .Replace("{{Value}}", value, StringComparison.Ordinal);
    }

    private static string RenderCategoryLine(
        TextTemplate textTemplate,
        string categoryKey,
        string categoryDisplayName,
        string outputFileName,
        int count)
    {
        return textTemplate.RunSummaryCategoryLineTemplate
            .Replace("{{CategoryKey}}", categoryKey, StringComparison.Ordinal)
            .Replace("{{CategoryDisplayName}}", categoryDisplayName, StringComparison.Ordinal)
            .Replace("{{OutputFileName}}", outputFileName, StringComparison.Ordinal)
            .Replace("{{Count}}", count.ToString(), StringComparison.Ordinal);
    }
}
