using Bragi.Infrastructure.Workflow;

namespace Bragi.Tests.Workflow;

public sealed class StepNavigationServiceTests
{
    [Fact]
    public void Initialize_MoveNext_MovePrevious_AndGoTo_WorkCorrectly()
    {
        var service = new StepNavigationService();

        service.Initialize(5);

        Assert.Equal(0, service.CurrentStepIndex);
        Assert.Equal(5, service.TotalStepCount);
        Assert.True(service.CanMoveNext);
        Assert.False(service.CanMovePrevious);

        service.MoveNext();
        Assert.Equal(1, service.CurrentStepIndex);
        Assert.True(service.CanMovePrevious);

        service.GoTo(4);
        Assert.Equal(4, service.CurrentStepIndex);
        Assert.False(service.CanMoveNext);

        service.MovePrevious();
        Assert.Equal(3, service.CurrentStepIndex);
    }

    [Fact]
    public void GoTo_InvalidStep_Throws()
    {
        var service = new StepNavigationService();
        service.Initialize(5);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.GoTo(5));
    }
}
