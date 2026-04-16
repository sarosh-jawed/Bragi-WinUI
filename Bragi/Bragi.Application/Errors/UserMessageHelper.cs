
namespace Bragi.Application.Errors;

/// <summary>
/// Maps internal exceptions to short, user-friendly messages.
/// Technical details belong in logs, not in the UI.
/// </summary>
public static class UserMessageHelper
{
    public static string ForInputLoad(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return "Input loading was cancelled. No changes were lost.";
        }

        if (exception is FileNotFoundException)
        {
            return "The selected file could not be found. Please choose the file again.";
        }

        if (exception is UnauthorizedAccessException)
        {
            return "Bragi could not access the selected file. Please check permissions and try again.";
        }

        if (exception is IOException)
        {
            return "Bragi could not read the selected file. Please close other programs using it and try again.";
        }

        if (exception is InvalidOperationException)
        {
            return "The selected file could not be processed. Please verify the file format and configured input columns.";
        }

        return "Unable to load the selected input file. Please try again and check the log folder if the issue continues.";
    }

    public static string ForPreview(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return "Preview generation was cancelled. No changes were lost.";
        }

        if (exception is InvalidOperationException)
        {
            return "Preview could not be generated. Please confirm the input and subject review steps are complete.";
        }

        return "Unable to generate preview results. Please try again and check the log folder if the issue continues.";
    }

    public static string ForExport(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return "Export was cancelled. No files were removed.";
        }

        if (exception is UnauthorizedAccessException)
        {
            return "Bragi could not write to the selected output folder. Please choose another folder or check permissions.";
        }

        if (exception is IOException)
        {
            return "Bragi could not finish writing output files. Please close any open output files and try again.";
        }

        return "Export could not be completed. Please try again or check the log folder for details.";
    }

    public static string ForFolderOpen(Exception exception)
    {
        if (exception is UnauthorizedAccessException)
        {
            return "Bragi could not open that folder because access was denied.";
        }

        if (exception is IOException)
        {
            return "Bragi could not open that folder right now. Please try again.";
        }

        return "Bragi could not open the requested folder.";
    }
}
