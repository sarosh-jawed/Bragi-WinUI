using System;

namespace Bragi.Infrastructure.Configuration;

public sealed class PathTokenResolver
{
    public string Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        return path.Trim()
            .Replace("%LOCALAPPDATA%", localAppDataPath, StringComparison.OrdinalIgnoreCase)
            .Replace("%USERPROFILE%", userProfilePath, StringComparison.OrdinalIgnoreCase)
            .Replace("%DOCUMENTS%", documentsPath, StringComparison.OrdinalIgnoreCase);
    }
}
