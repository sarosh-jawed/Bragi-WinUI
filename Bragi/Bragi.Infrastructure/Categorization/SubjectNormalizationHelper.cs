using System;
using System.Linq;
using Bragi.Application.Configuration;

namespace Bragi.Infrastructure.Categorization;

public sealed class SubjectNormalizationHelper
{
    public string NormalizeForMatching(string? value, BehaviorOptions behaviorOptions)
    {
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var workingValue = behaviorOptions.TrimSubjects
            ? value.Trim()
            : value;

        var normalizedCharacters = workingValue
            .Select(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) ? character : ' ')
            .ToArray();

        var normalizedValue = new string(normalizedCharacters);

        if (behaviorOptions.NormalizeWhitespace)
        {
            normalizedValue = CollapseWhitespace(normalizedValue);
        }

        if (behaviorOptions.TrimSubjects)
        {
            normalizedValue = normalizedValue.Trim();
        }

        if (behaviorOptions.CaseInsensitiveMatching)
        {
            normalizedValue = normalizedValue.ToLowerInvariant();
        }

        return normalizedValue;
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(
            " ",
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
