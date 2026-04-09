using System.IO;

namespace Bragi.Domain.ValueObjects;

public readonly record struct OutputFileName
{
    public OutputFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Output file name cannot be null, empty, or whitespace.", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Output file name contains invalid file name characters.", nameof(value));
        }

        if (trimmedValue.Contains(Path.DirectorySeparatorChar) || trimmedValue.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Output file name must not contain directory separator characters.", nameof(value));
        }

        Value = trimmedValue;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
