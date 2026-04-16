using Bragi.Application.Errors;

namespace Bragi.Tests.UI;

public sealed class UserMessageHelperTests
{
    [Fact]
    public void ForInputLoad_ReturnsFriendlyMessage_ForFileNotFound()
    {
        var message = UserMessageHelper.ForInputLoad(new FileNotFoundException("raw internal text"));

        Assert.Equal("The selected file could not be found. Please choose the file again.", message);
    }

    [Fact]
    public void ForPreview_ReturnsFriendlyMessage_ForCancellation()
    {
        var message = UserMessageHelper.ForPreview(new OperationCanceledException());

        Assert.Equal("Preview generation was cancelled. No changes were lost.", message);
    }

    [Fact]
    public void ForExport_ReturnsFriendlyMessage_ForUnauthorizedAccess()
    {
        var message = UserMessageHelper.ForExport(new UnauthorizedAccessException("raw internal text"));

        Assert.Equal("Bragi could not write to the selected output folder. Please choose another folder or check permissions.", message);
    }

    [Fact]
    public void ForFolderOpen_ReturnsGenericFriendlyFallback_ForUnknownException()
    {
        var message = UserMessageHelper.ForFolderOpen(new Exception("raw internal text"));

        Assert.Equal("Bragi could not open the requested folder.", message);
    }
}
