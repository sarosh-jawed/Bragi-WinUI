using System;
using Bragi.Application.Contracts;

namespace Bragi.Infrastructure.Workflow;

public sealed class StepNavigationService : IStepNavigationService
{
    private bool _isInitialized;

    public int CurrentStepIndex { get; private set; }

    public int TotalStepCount { get; private set; }

    public bool CanMoveNext =>
        _isInitialized &&
        CurrentStepIndex < TotalStepCount - 1;

    public bool CanMovePrevious =>
        _isInitialized &&
        CurrentStepIndex > 0;

    public void Initialize(int totalStepCount, int startingStepIndex = 0)
    {
        if (totalStepCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalStepCount), "Total step count must be at least 1.");
        }

        if (startingStepIndex < 0 || startingStepIndex >= totalStepCount)
        {
            throw new ArgumentOutOfRangeException(nameof(startingStepIndex), "Starting step index is outside the valid range.");
        }

        TotalStepCount = totalStepCount;
        CurrentStepIndex = startingStepIndex;
        _isInitialized = true;
    }

    public void MoveNext()
    {
        EnsureInitialized();

        if (!CanMoveNext)
        {
            throw new InvalidOperationException("Cannot move to the next step.");
        }

        CurrentStepIndex++;
    }

    public void MovePrevious()
    {
        EnsureInitialized();

        if (!CanMovePrevious)
        {
            throw new InvalidOperationException("Cannot move to the previous step.");
        }

        CurrentStepIndex--;
    }

    public void GoTo(int stepIndex)
    {
        EnsureInitialized();

        if (stepIndex < 0 || stepIndex >= TotalStepCount)
        {
            throw new ArgumentOutOfRangeException(nameof(stepIndex), "Step index is outside the valid range.");
        }

        CurrentStepIndex = stepIndex;
    }

    public void Reset()
    {
        CurrentStepIndex = 0;
        TotalStepCount = 0;
        _isInitialized = false;
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Step navigation service has not been initialized.");
        }
    }
}
