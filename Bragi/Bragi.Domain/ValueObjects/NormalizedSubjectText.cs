namespace Bragi.Domain.ValueObjects;

public readonly record struct NormalizedSubjectText
{
    public NormalizedSubjectText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Normalized subject text cannot be null, empty, or whitespace.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
