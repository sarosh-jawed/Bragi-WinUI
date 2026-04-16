using Bragi.Application.Workflow;

namespace Bragi.Tests.Workflow;

public sealed class WizardStateTests
{
    [Fact]
    public void CreateInitial_UsesFiveStepShellModelByDefault()
    {
        var state = WizardState.CreateInitial();

        Assert.Equal(5, state.TotalStepCount);
        Assert.Equal(0, state.CurrentStepIndex);

        Assert.False(state.IsStepLocked(0));
        Assert.False(state.IsStepLocked(1));
        Assert.True(state.IsStepLocked(2));
        Assert.True(state.IsStepLocked(3));
        Assert.True(state.IsStepLocked(4));

        Assert.False(state.IsInputLoaded);
        Assert.False(state.IsExtractionReviewComplete);
        Assert.False(state.HasPreview);
        Assert.False(state.IsExportComplete);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public void CreateInitial_AllowsExplicitStepCountOverride()
    {
        var state = WizardState.CreateInitial(3);

        Assert.Equal(3, state.TotalStepCount);
        Assert.Equal(0, state.CurrentStepIndex);

        Assert.False(state.IsStepLocked(0));
        Assert.False(state.IsStepLocked(1));
        Assert.True(state.IsStepLocked(2));
    }

    [Fact]
    public void CreateInitial_Throws_WhenStepCountIsLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WizardState.CreateInitial(0));
    }
}
