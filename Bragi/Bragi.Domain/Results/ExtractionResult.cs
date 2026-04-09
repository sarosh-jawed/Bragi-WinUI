using Bragi.Domain.Enums;
using Bragi.Domain.Models;

namespace Bragi.Domain.Results;

public sealed record ExtractionResult
{
    public ExtractionResult(
        string sourceFile,
        InputFileKind inputFileKind,
        IReadOnlyList<ExtractedSubject> subjects,
        int totalRecordsRead,
        int blankOrIgnoredCount,
        int duplicateCount,
        int parseWarningCount)
    {
        if (string.IsNullOrWhiteSpace(sourceFile))
        {
            throw new ArgumentException("Source file cannot be null, empty, or whitespace.", nameof(sourceFile));
        }

        if (inputFileKind == InputFileKind.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(inputFileKind), "Input file kind must be known.");
        }

        if (subjects is null)
        {
            throw new ArgumentNullException(nameof(subjects));
        }

        if (totalRecordsRead < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRecordsRead), "Total records read cannot be negative.");
        }

        if (blankOrIgnoredCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blankOrIgnoredCount), "Blank or ignored count cannot be negative.");
        }

        if (duplicateCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duplicateCount), "Duplicate count cannot be negative.");
        }

        if (parseWarningCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parseWarningCount), "Parse warning count cannot be negative.");
        }

        SourceFile = sourceFile.Trim();
        InputFileKind = inputFileKind;
        Subjects = subjects.ToArray();
        TotalRecordsRead = totalRecordsRead;
        BlankOrIgnoredCount = blankOrIgnoredCount;
        DuplicateCount = duplicateCount;
        ParseWarningCount = parseWarningCount;
    }

    public string SourceFile { get; }

    public InputFileKind InputFileKind { get; }

    public IReadOnlyList<ExtractedSubject> Subjects { get; }

    public int TotalRecordsRead { get; }

    public int BlankOrIgnoredCount { get; }

    public int DuplicateCount { get; }

    public int ParseWarningCount { get; }

    public int ExtractedCount => Subjects.Count;
}
