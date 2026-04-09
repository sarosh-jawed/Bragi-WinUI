using System.Collections.ObjectModel;
using Bragi.Domain.Enums;
using Bragi.Domain.ValueObjects;

namespace Bragi.Domain.Results;

public sealed record RunSummary
{
    public RunSummary(
        string sourceFile,
        InputFileKind inputFileKind,
        DateTimeOffset runStartedAtUtc,
        DateTimeOffset runCompletedAtUtc,
        int totalRecordsRead,
        int extractedSubjectCount,
        int categorizedAssignmentCount,
        int uncategorizedSubjectCount,
        int blankOrIgnoredCount,
        int duplicateCount,
        int parseWarningCount,
        IReadOnlyDictionary<CategoryKey, int> categoryCounts)
    {
        if (string.IsNullOrWhiteSpace(sourceFile))
        {
            throw new ArgumentException("Source file cannot be null, empty, or whitespace.", nameof(sourceFile));
        }

        if (inputFileKind == InputFileKind.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(inputFileKind), "Input file kind must be known.");
        }

        if (runCompletedAtUtc < runStartedAtUtc)
        {
            throw new ArgumentException("Run completed time cannot be earlier than run started time.", nameof(runCompletedAtUtc));
        }

        if (totalRecordsRead < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRecordsRead), "Total records read cannot be negative.");
        }

        if (extractedSubjectCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extractedSubjectCount), "Extracted subject count cannot be negative.");
        }

        if (categorizedAssignmentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categorizedAssignmentCount), "Categorized assignment count cannot be negative.");
        }

        if (uncategorizedSubjectCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(uncategorizedSubjectCount), "Uncategorized subject count cannot be negative.");
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

        if (categoryCounts is null)
        {
            throw new ArgumentNullException(nameof(categoryCounts));
        }

        if (categoryCounts.Any(pair => pair.Value < 0))
        {
            throw new ArgumentException("Category counts cannot contain negative values.", nameof(categoryCounts));
        }

        SourceFile = sourceFile.Trim();
        InputFileKind = inputFileKind;
        RunStartedAtUtc = runStartedAtUtc;
        RunCompletedAtUtc = runCompletedAtUtc;
        TotalRecordsRead = totalRecordsRead;
        ExtractedSubjectCount = extractedSubjectCount;
        CategorizedAssignmentCount = categorizedAssignmentCount;
        UncategorizedSubjectCount = uncategorizedSubjectCount;
        BlankOrIgnoredCount = blankOrIgnoredCount;
        DuplicateCount = duplicateCount;
        ParseWarningCount = parseWarningCount;
        CategoryCounts = new ReadOnlyDictionary<CategoryKey, int>(new Dictionary<CategoryKey, int>(categoryCounts));
    }

    public string SourceFile { get; }

    public InputFileKind InputFileKind { get; }

    public DateTimeOffset RunStartedAtUtc { get; }

    public DateTimeOffset RunCompletedAtUtc { get; }

    public int TotalRecordsRead { get; }

    public int ExtractedSubjectCount { get; }

    public int CategorizedAssignmentCount { get; }

    public int UncategorizedSubjectCount { get; }

    public int BlankOrIgnoredCount { get; }

    public int DuplicateCount { get; }

    public int ParseWarningCount { get; }

    public IReadOnlyDictionary<CategoryKey, int> CategoryCounts { get; }

    public TimeSpan Duration => RunCompletedAtUtc - RunStartedAtUtc;
}
