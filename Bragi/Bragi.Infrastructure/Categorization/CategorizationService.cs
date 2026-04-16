using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Domain.Models;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Bragi.Infrastructure.Categorization;

public sealed class CategorizationService : ICategorizationService
{
    private readonly ILogger<CategorizationService> _logger;
    private readonly SubjectNormalizationHelper _subjectNormalizationHelper;
    private readonly KeywordMatcher _keywordMatcher;
    private readonly ExclusionMatcher _exclusionMatcher;

    public CategorizationService(
        ILogger<CategorizationService> logger,
        SubjectNormalizationHelper subjectNormalizationHelper,
        KeywordMatcher keywordMatcher,
        ExclusionMatcher exclusionMatcher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subjectNormalizationHelper = subjectNormalizationHelper ?? throw new ArgumentNullException(nameof(subjectNormalizationHelper));
        _keywordMatcher = keywordMatcher ?? throw new ArgumentNullException(nameof(keywordMatcher));
        _exclusionMatcher = exclusionMatcher ?? throw new ArgumentNullException(nameof(exclusionMatcher));
    }

    public Task<CategorizationResult> CategorizeAsync(
        ExtractionResult extractionResult,
        IReadOnlyList<CategoryRule> categoryRules,
        BehaviorOptions behaviorOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(extractionResult);
        ArgumentNullException.ThrowIfNull(categoryRules);
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        var orderedRules = categoryRules
            .Where(rule => rule.Enabled)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation(
            "Starting subject categorization. ExtractedSubjectCount={ExtractedSubjectCount} EnabledCategoryRuleCount={EnabledCategoryRuleCount} AllowMultiMatch={AllowMultiMatch}",
            extractionResult.ExtractedCount,
            orderedRules.Length,
            behaviorOptions.AllowMultiMatch);

        var categorizedSubjects = new List<CategorizedSubject>();
        var uncategorizedSubjects = new List<UncategorizedSubject>();
        var categoryCounts = new Dictionary<CategoryKey, int>();

        foreach (var extractedSubject in extractionResult.Subjects.OrderBy(subject => subject.SequenceNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedSubject = _subjectNormalizationHelper.NormalizeForMatching(
                extractedSubject.Entry.OriginalSubject.Value,
                behaviorOptions);

            if (string.IsNullOrWhiteSpace(normalizedSubject))
            {
                uncategorizedSubjects.Add(new UncategorizedSubject(
                    extractedSubject,
                    "Subject is blank after normalization."));

                continue;
            }

            var categoryMatches = new List<CategoryMatch>();
            var candidateFailureReasons = new List<string>();

            foreach (var rule in orderedRules)
            {
                if (!_keywordMatcher.TryMatch(
                        normalizedSubject,
                        rule.IncludeKeywords,
                        rule.IncludeMatchMode,
                        behaviorOptions,
                        out var matchedIncludeKeyword))
                {
                    continue;
                }

                if (rule.RequireAnyKeywords.Count > 0 &&
                    !_keywordMatcher.TryMatch(
                        normalizedSubject,
                        rule.RequireAnyKeywords,
                        rule.RequireAnyMatchMode,
                        behaviorOptions,
                        out var matchedRequiredKeyword))
                {
                    candidateFailureReasons.Add(
                        $"Rule '{rule.Key}' matched include keywords but did not satisfy required keywords.");
                    continue;
                }

                var exclusionReason = _exclusionMatcher.GetExclusionReason(
                    normalizedSubject,
                    rule,
                    behaviorOptions);

                if (!string.IsNullOrWhiteSpace(exclusionReason))
                {
                    candidateFailureReasons.Add(exclusionReason);
                    continue;
                }

                var routingReason = BuildRoutingReason(
                    matchedIncludeKeyword,
                    rule.RequireAnyKeywords.Count > 0
                        ? rule.RequireAnyKeywords.FirstOrDefault(requiredKeyword =>
                            _keywordMatcher.TryMatch(
                                normalizedSubject,
                                new[] { requiredKeyword },
                                rule.RequireAnyMatchMode,
                                behaviorOptions,
                                out _))
                        : null);

                categoryMatches.Add(new CategoryMatch(
                    new CategoryKey(rule.Key),
                    new OutputFileName(rule.OutputFileName),
                    routingReason));

                if (!behaviorOptions.AllowMultiMatch)
                {
                    break;
                }
            }

            if (categoryMatches.Count > 0)
            {
                categorizedSubjects.Add(new CategorizedSubject(extractedSubject, categoryMatches));

                foreach (var categoryMatch in categoryMatches)
                {
                    if (categoryCounts.TryGetValue(categoryMatch.CategoryKey, out var currentCount))
                    {
                        categoryCounts[categoryMatch.CategoryKey] = currentCount + 1;
                    }
                    else
                    {
                        categoryCounts[categoryMatch.CategoryKey] = 1;
                    }
                }

                continue;
            }

            var uncategorizedReason = candidateFailureReasons.Count > 0
                ? candidateFailureReasons[0]
                : "No configured category matched.";

            uncategorizedSubjects.Add(new UncategorizedSubject(
                extractedSubject,
                uncategorizedReason));
        }

        _logger.LogInformation(
            "Categorization completed. Categorized subjects: {CategorizedSubjectCount}. Uncategorized subjects: {UncategorizedSubjectCount}. Total assignments: {TotalAssignments}.",
            categorizedSubjects.Count,
            uncategorizedSubjects.Count,
            categoryCounts.Values.Sum());

        return Task.FromResult(new CategorizationResult(
            categorizedSubjects,
            uncategorizedSubjects,
            categoryCounts));
    }

    private static string BuildRoutingReason(
        string? matchedIncludeKeyword,
        string? matchedRequiredKeyword)
    {
        if (!string.IsNullOrWhiteSpace(matchedIncludeKeyword) &&
            !string.IsNullOrWhiteSpace(matchedRequiredKeyword))
        {
            return $"Matched include keyword: {matchedIncludeKeyword}; matched required keyword: {matchedRequiredKeyword}";
        }

        if (!string.IsNullOrWhiteSpace(matchedIncludeKeyword))
        {
            return $"Matched include keyword: {matchedIncludeKeyword}";
        }

        return "Matched configured category rule.";
    }
}
