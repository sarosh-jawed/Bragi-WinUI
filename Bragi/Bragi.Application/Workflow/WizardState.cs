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

    public static WizardState CreateInitial(int totalStepCount = 4)
    {
        if (totalStepCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalStepCount), "Total step count must be at least 1.");
        }

        return new WizardState
        {
            TotalStepCount = totalStepCount,
            CurrentStepIndex = 0,
            LockedStepIndices = Enumerable.Range(1, Math.Max(0, totalStepCount - 1)).ToArray(),
            IsInputLoaded = false,
            IsExtractionReviewComplete = false,
            HasPreview = false,
            IsExportComplete = false,
            IsBusy = false
        };
    }
}
