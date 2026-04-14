using System;
using System.Collections.Generic;
using System.Linq;
using Bragi.Application.Configuration;

namespace Bragi.Infrastructure.Categorization;

public sealed class KeywordMatcher
{
    private readonly SubjectNormalizationHelper _subjectNormalizationHelper;

    public KeywordMatcher(SubjectNormalizationHelper subjectNormalizationHelper)
    {
        _subjectNormalizationHelper = subjectNormalizationHelper ?? throw new ArgumentNullException(nameof(subjectNormalizationHelper));
    }

    public bool TryMatch(
        string normalizedSubject,
        IEnumerable<string>? keywords,
        CategoryMatchMode matchMode,
        BehaviorOptions behaviorOptions,
        out string? matchedKeyword)
    {
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        matchedKeyword = null;

        if (string.IsNullOrWhiteSpace(normalizedSubject) || keywords is null)
        {
            return false;
        }

        foreach (var rawKeyword in keywords)
        {
            var normalizedKeyword = _subjectNormalizationHelper.NormalizeForMatching(rawKeyword, behaviorOptions);

            if (string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                continue;
            }

            if (IsMatch(normalizedSubject, normalizedKeyword, matchMode))
            {
                matchedKeyword = rawKeyword?.Trim();
                return true;
            }
        }

        return false;
    }

    public bool ContainsWholeWord(
        string normalizedSubject,
        string rawKeyword,
        BehaviorOptions behaviorOptions)
    {
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        if (string.IsNullOrWhiteSpace(normalizedSubject) || string.IsNullOrWhiteSpace(rawKeyword))
        {
            return false;
        }

        var normalizedKeyword = _subjectNormalizationHelper.NormalizeForMatching(rawKeyword, behaviorOptions);

        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return false;
        }

        return IsWholeWordMatch(normalizedSubject, normalizedKeyword);
    }

    private static bool IsMatch(
        string normalizedSubject,
        string normalizedKeyword,
        CategoryMatchMode matchMode)
    {
        return matchMode switch
        {
            CategoryMatchMode.Contains => normalizedSubject.Contains(normalizedKeyword, StringComparison.Ordinal),
            CategoryMatchMode.Exact => string.Equals(normalizedSubject, normalizedKeyword, StringComparison.Ordinal),
            CategoryMatchMode.WholeWord => IsWholeWordMatch(normalizedSubject, normalizedKeyword),
            _ => false
        };
    }

    private static bool IsWholeWordMatch(
        string normalizedSubject,
        string normalizedKeyword)
    {
        var subjectTokens = normalizedSubject
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var keywordTokens = normalizedKeyword
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (keywordTokens.Length == 0 || subjectTokens.Length < keywordTokens.Length)
        {
            return false;
        }

        for (var subjectIndex = 0; subjectIndex <= subjectTokens.Length - keywordTokens.Length; subjectIndex++)
        {
            var allTokensMatched = true;

            for (var keywordIndex = 0; keywordIndex < keywordTokens.Length; keywordIndex++)
            {
                if (!string.Equals(
                        subjectTokens[subjectIndex + keywordIndex],
                        keywordTokens[keywordIndex],
                        StringComparison.Ordinal))
                {
                    allTokensMatched = false;
                    break;
                }
            }

            if (allTokensMatched)
            {
                return true;
            }
        }

        return false;
    }
}
