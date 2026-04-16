using System;
using System.Linq;

namespace Bragi.Application.Workflow;

public sealed record WizardState
{
    public int TotalStepCount { get; init; }

    public int CurrentStepIndex { get; init; }

    public IReadOnlyList<int> LockedStepIndices { get; init; } = Array.Empty<int>();

    public bool IsInputLoaded { get; init; }

    public bool IsExtractionReviewComplete { get; init; }

    public bool HasPreview { get; init; }

    public bool IsExportComplete { get; init; }

    public bool IsBusy { get; init; }

    public bool IsStepLocked(int stepIndex)
    {
        return LockedStepIndices.Contains(stepIndex);
    }

    public static WizardState CreateInitial(int totalStepCount = 5)
    {
        if (totalStepCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalStepCount), "Total step count must be at least 1.");
        }

        return new WizardState
        {
            TotalStepCount = totalStepCount,
            CurrentStepIndex = 0,
            LockedStepIndices = totalStepCount <= 2
                ? Array.Empty<int>()
                : Enumerable.Range(2, totalStepCount - 2).ToArray(),
            IsInputLoaded = false,
            IsExtractionReviewComplete = false,
            HasPreview = false,
            IsExportComplete = false,
            IsBusy = false
        };
    }
}
