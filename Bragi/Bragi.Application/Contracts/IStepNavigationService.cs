namespace Bragi.Application.Contracts;

public interface IStepNavigationService
{
    int CurrentStepIndex { get; }

    int TotalStepCount { get; }

    bool CanMoveNext { get; }

    bool CanMovePrevious { get; }

    void Initialize(int totalStepCount, int startingStepIndex = 0);

    void MoveNext();

    void MovePrevious();

    void GoTo(int stepIndex);

    void Reset();
}
