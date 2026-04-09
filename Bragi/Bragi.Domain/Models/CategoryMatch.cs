using Bragi.Domain.ValueObjects;

namespace Bragi.Domain.Models;

public sealed record CategoryMatch
{
    public CategoryMatch(
        CategoryKey categoryKey,
        OutputFileName outputFileName,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Category match reason cannot be null, empty, or whitespace.", nameof(reason));
        }

        CategoryKey = categoryKey;
        OutputFileName = outputFileName;
        Reason = reason.Trim();
    }

    public CategoryKey CategoryKey { get; }

    public OutputFileName OutputFileName { get; }

    public string Reason { get; }
}
