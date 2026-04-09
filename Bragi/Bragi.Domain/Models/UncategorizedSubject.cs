namespace Bragi.Domain.Models;

public sealed record UncategorizedSubject
{
    public UncategorizedSubject(
        ExtractedSubject subject,
        string reason)
    {
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Uncategorized reason cannot be null, empty, or whitespace.", nameof(reason));
        }

        Reason = reason.Trim();
    }

    public ExtractedSubject Subject { get; }

    public string Reason { get; }
}
