using Bragi.Application.Configuration;
using Bragi.Domain.Models;
using Bragi.Domain.Results;

namespace Bragi.Infrastructure.Export;

public sealed class TextBodyBuilder
{
    public IReadOnlyList<string> BuildCategoryLines(
        CategorizationResult categorizationResult,
        CategoryRule categoryRule,
        TextTemplate textTemplate,
        BehaviorOptions behaviorOptions)
    {
        ArgumentNullException.ThrowIfNull(categorizationResult);
        ArgumentNullException.ThrowIfNull(categoryRule);
        ArgumentNullException.ThrowIfNull(textTemplate);
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        var lines = new List<string>();

        foreach (var categorizedSubject in categorizationResult.CategorizedSubjects.OrderBy(subject => subject.Subject.SequenceNumber))
        {
            var categoryMatch = categorizedSubject.Matches.FirstOrDefault(match =>
                string.Equals(match.CategoryKey.Value, categoryRule.Key, StringComparison.OrdinalIgnoreCase));

            if (categoryMatch is null)
            {
                continue;
            }

            lines.Add(RenderTemplate(
                textTemplate.CategoryLineTemplate,
                categorizedSubject.Subject,
                categoryRule,
                categoryMatch.Reason,
                null));
        }

        return FinalizeLines(lines, behaviorOptions);
    }

    public IReadOnlyList<string> BuildUncategorizedLines(
        CategorizationResult categorizationResult,
        TextTemplate textTemplate,
        BehaviorOptions behaviorOptions)
    {
        ArgumentNullException.ThrowIfNull(categorizationResult);
        ArgumentNullException.ThrowIfNull(textTemplate);
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        var lines = new List<string>();

        foreach (var uncategorizedSubject in categorizationResult.UncategorizedSubjects.OrderBy(subject => subject.Subject.SequenceNumber))
        {
            lines.Add(RenderTemplate(
                textTemplate.UncategorizedLineTemplate,
                uncategorizedSubject.Subject,
                null,
                null,
                uncategorizedSubject.Reason));
        }

        return FinalizeLines(lines, behaviorOptions);
    }

    private static IReadOnlyList<string> FinalizeLines(
        IEnumerable<string> lines,
        BehaviorOptions behaviorOptions)
    {
        IEnumerable<string> outputLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.TrimEnd());

        if (behaviorOptions.DeduplicateOutputs)
        {
            outputLines = outputLines.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        if (behaviorOptions.SortOutputs)
        {
            outputLines = outputLines.OrderBy(line => line, StringComparer.OrdinalIgnoreCase);
        }

        return outputLines.ToArray();
    }

    private static string RenderTemplate(
        ExtractedSubject extractedSubject,
        string template,
        CategoryRule? categoryRule,
        string? routingReason,
        string? uncategorizedReason)
    {
        var renderedValue = template;

        renderedValue = ReplaceToken(renderedValue, "{{OriginalSubject}}", extractedSubject.Entry.OriginalSubject.Value);
        renderedValue = ReplaceToken(renderedValue, "{{NormalizedSubject}}", extractedSubject.Entry.NormalizedSubject.Value);
        renderedValue = ReplaceToken(renderedValue, "{{SourceFile}}", extractedSubject.Entry.SourceFile);
        renderedValue = ReplaceToken(renderedValue, "{{SourceRowNumber}}", extractedSubject.Entry.SourceRowNumber?.ToString() ?? string.Empty);
        renderedValue = ReplaceToken(renderedValue, "{{SourceTitle}}", extractedSubject.Entry.SourceTitle ?? string.Empty);
        renderedValue = ReplaceToken(renderedValue, "{{SourceRecordId}}", extractedSubject.Entry.SourceRecordId ?? string.Empty);
        renderedValue = ReplaceToken(renderedValue, "{{RoutingReason}}", routingReason ?? string.Empty);
        renderedValue = ReplaceToken(renderedValue, "{{Reason}}", uncategorizedReason ?? string.Empty);
        renderedValue = ReplaceToken(renderedValue, "{{CategoryKey}}", categoryRule?.Key ?? string.Empty);
        renderedValue = ReplaceToken(renderedValue, "{{CategoryDisplayName}}", categoryRule?.DisplayName ?? string.Empty);
        renderedValue = ReplaceToken(renderedValue, "{{OutputFileName}}", categoryRule?.OutputFileName ?? string.Empty);

        return renderedValue;
    }

    private static string RenderTemplate(
        string template,
        ExtractedSubject extractedSubject,
        CategoryRule? categoryRule,
        string? routingReason,
        string? uncategorizedReason)
    {
        return RenderTemplate(
            extractedSubject,
            template,
            categoryRule,
            routingReason,
            uncategorizedReason);
    }

    private static string ReplaceToken(
        string template,
        string token,
        string value)
    {
        return template.Replace(token, value, StringComparison.Ordinal);
    }
}
