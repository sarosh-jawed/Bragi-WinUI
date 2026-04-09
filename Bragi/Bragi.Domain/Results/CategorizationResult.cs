using System.Collections.ObjectModel;
using Bragi.Domain.Models;
using Bragi.Domain.ValueObjects;

namespace Bragi.Domain.Results;

public sealed record CategorizationResult
{
    public CategorizationResult(
        IReadOnlyList<CategorizedSubject> categorizedSubjects,
        IReadOnlyList<UncategorizedSubject> uncategorizedSubjects,
        IReadOnlyDictionary<CategoryKey, int> categoryCounts)
    {
        if (categorizedSubjects is null)
        {
            throw new ArgumentNullException(nameof(categorizedSubjects));
        }

        if (uncategorizedSubjects is null)
        {
            throw new ArgumentNullException(nameof(uncategorizedSubjects));
        }

        if (categoryCounts is null)
        {
            throw new ArgumentNullException(nameof(categoryCounts));
        }

        if (categoryCounts.Any(pair => pair.Value < 0))
        {
            throw new ArgumentException("Category counts cannot contain negative values.", nameof(categoryCounts));
        }

        CategorizedSubjects = categorizedSubjects.ToArray();
        UncategorizedSubjects = uncategorizedSubjects.ToArray();
        CategoryCounts = new ReadOnlyDictionary<CategoryKey, int>(new Dictionary<CategoryKey, int>(categoryCounts));
    }

    public IReadOnlyList<CategorizedSubject> CategorizedSubjects { get; }

    public IReadOnlyList<UncategorizedSubject> UncategorizedSubjects { get; }

    public IReadOnlyDictionary<CategoryKey, int> CategoryCounts { get; }

    public int CategorizedSubjectCount => CategorizedSubjects.Count;

    public int UncategorizedSubjectCount => UncategorizedSubjects.Count;

    public int TotalProcessedSubjects => CategorizedSubjectCount + UncategorizedSubjectCount;

    public int TotalAssignments => CategorizedSubjects.Sum(subject => subject.AssignmentCount);
}
