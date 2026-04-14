namespace Bragi.Application.Configuration;

public sealed record TextTemplate
{
    public string CategoryLineTemplate { get; init; } = "{{OriginalSubject}}";

    public string UncategorizedLineTemplate { get; init; } = "{{OriginalSubject}}";

    public string RunSummaryHeaderTemplate { get; init; } = "Bragi Run Summary";

    public string RunSummaryCategoryLineTemplate { get; init; } = "{{CategoryKey}}: {{Count}}";

    public string RunSummaryDetailLineTemplate { get; init; } = "{{Label}}: {{Value}}";
}
