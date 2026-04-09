namespace Bragi.Domain.Models;

public sealed record CategorizedSubject
{
    public CategorizedSubject(
        ExtractedSubject subject,
        IReadOnlyList<CategoryMatch> matches)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));

        if (matches is null)
        {
            throw new ArgumentNullException(nameof(matches));
        }

        var materializedMatches = matches.ToArray();

        if (materializedMatches.Length == 0)
        {
            throw new ArgumentException("Categorized subject must contain at least one category match.", nameof(matches));
        }

        Matches = materializedMatches;
    }

    public ExtractedSubject Subject { get; }

    public IReadOnlyList<CategoryMatch> Matches { get; }

    public int AssignmentCount => Matches.Count;
}
