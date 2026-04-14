using System;
using Bragi.Application.Configuration;

namespace Bragi.Infrastructure.Categorization;

public sealed class ExclusionMatcher
{
    private readonly KeywordMatcher _keywordMatcher;

    public ExclusionMatcher(KeywordMatcher keywordMatcher)
    {
        _keywordMatcher = keywordMatcher ?? throw new ArgumentNullException(nameof(keywordMatcher));
    }

    public string? GetExclusionReason(
        string normalizedSubject,
        CategoryRule categoryRule,
        BehaviorOptions behaviorOptions)
    {
        ArgumentNullException.ThrowIfNull(categoryRule);
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        if (string.IsNullOrWhiteSpace(normalizedSubject))
        {
            return null;
        }

        if (categoryRule.DisableForFiction &&
            _keywordMatcher.ContainsWholeWord(normalizedSubject, "fiction", behaviorOptions))
        {
            return "Excluded because subject contains fiction.";
        }

        if (categoryRule.DisableForJuvenile &&
            _keywordMatcher.ContainsWholeWord(normalizedSubject, "juvenile", behaviorOptions))
        {
            return "Excluded because subject contains juvenile.";
        }

        if (_keywordMatcher.TryMatch(
                normalizedSubject,
                categoryRule.ExcludeKeywords,
                categoryRule.ExcludeMatchMode,
                behaviorOptions,
                out var matchedExcludeKeyword))
        {
            return $"Excluded because subject matched exclude keyword: {matchedExcludeKeyword}";
        }

        return null;
    }
}
