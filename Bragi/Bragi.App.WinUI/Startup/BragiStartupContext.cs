namespace Bragi.App.WinUI.Startup;

public sealed class BragiStartupContext
{
    public BragiStartupContext(
        string packagedConfigPath,
        string localConfigPath,
        string logsRoot,
        string outputRoot)
    {
        PackagedConfigPath = packagedConfigPath;
        LocalConfigPath = localConfigPath;
        LogsRoot = logsRoot;
        OutputRoot = outputRoot;
    }

    public string PackagedConfigPath { get; }

    public string LocalConfigPath { get; }

    public string LogsRoot { get; }

    public string OutputRoot { get; }
}
