namespace Bragi.Domain.ValueObjects;

public readonly record struct CategoryKey
{
    public CategoryKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Category key cannot be null, empty, or whitespace.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
