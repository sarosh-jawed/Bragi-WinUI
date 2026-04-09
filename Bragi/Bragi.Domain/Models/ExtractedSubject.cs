using Bragi.Domain.ValueObjects;

namespace Bragi.Domain.Models;

public sealed record ExtractedSubject
{
    public ExtractedSubject(SubjectEntry entry, int sequenceNumber)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));

        if (sequenceNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "Sequence number must be greater than zero.");
        }

        SequenceNumber = sequenceNumber;
    }

    public SubjectEntry Entry { get; }

    public int SequenceNumber { get; }

    public SubjectText OriginalSubject => Entry.OriginalSubject;

    public NormalizedSubjectText NormalizedSubject => Entry.NormalizedSubject;
}
