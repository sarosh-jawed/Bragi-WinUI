namespace Bragi.Application.Configuration;

public sealed record Output
{
    public string RootPath { get; init; } = "%LOCALAPPDATA%\\Bragi\\Output";

    public string UncategorizedFileName { get; init; } = "NotCategorizedSubjects.txt";

    public string RunSummaryFileName { get; init; } = "RunSummary.txt";

    public bool CreateMonthlySubfolder { get; init; } = true;

    public string MonthlySubfolderFormat { get; init; } = "yyyy-MM";
}
