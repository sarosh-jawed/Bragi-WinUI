namespace Bragi.Application.Configuration;

public sealed record CsvColumns
{
    public string SubjectColumnName { get; init; } = "instance.subjects";

    public string? TitleColumnName { get; init; }

    public string? RecordIdColumnName { get; init; }

    public bool SubjectColumnContainsJsonArray { get; init; } = true;
}
