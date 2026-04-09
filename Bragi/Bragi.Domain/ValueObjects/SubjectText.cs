namespace Bragi.Domain.ValueObjects;

public readonly record struct SubjectText
{
    public SubjectText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Subject text cannot be null, empty, or whitespace.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
