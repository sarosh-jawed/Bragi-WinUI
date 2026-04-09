using Bragi.Domain.Enums;
using Bragi.Domain.ValueObjects;

namespace Bragi.Domain.Models;

public sealed record SubjectEntry
{
    public SubjectEntry(
        SubjectText originalSubject,
        NormalizedSubjectText normalizedSubject,
        string sourceFile,
        int? sourceRowNumber,
        string? sourceTitle,
        string? sourceRecordId,
        InputFileKind inputFileKind)
    {
        if (string.IsNullOrWhiteSpace(sourceFile))
        {
            throw new ArgumentException("Source file cannot be null, empty, or whitespace.", nameof(sourceFile));
        }

        if (sourceRowNumber.HasValue && sourceRowNumber.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRowNumber), "Source row number must be greater than zero when provided.");
        }

        if (inputFileKind == InputFileKind.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(inputFileKind), "Input file kind must be known.");
        }

        OriginalSubject = originalSubject;
        NormalizedSubject = normalizedSubject;
        SourceFile = sourceFile.Trim();
        SourceRowNumber = sourceRowNumber;
        SourceTitle = string.IsNullOrWhiteSpace(sourceTitle) ? null : sourceTitle.Trim();
        SourceRecordId = string.IsNullOrWhiteSpace(sourceRecordId) ? null : sourceRecordId.Trim();
        InputFileKind = inputFileKind;
    }

    public SubjectText OriginalSubject { get; }

    public NormalizedSubjectText NormalizedSubject { get; }

    public string SourceFile { get; }

    public int? SourceRowNumber { get; }

    public string? SourceTitle { get; }

    public string? SourceRecordId { get; }

    public InputFileKind InputFileKind { get; }

    public bool HasSourceRowNumber => SourceRowNumber.HasValue;

    public bool HasSourceTitle => !string.IsNullOrWhiteSpace(SourceTitle);

    public bool HasSourceRecordId => !string.IsNullOrWhiteSpace(SourceRecordId);
}
