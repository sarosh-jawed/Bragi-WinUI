namespace Bragi.Application.Configuration;

public sealed record InputOptions
{
    public List<string> PlainTextExtensions { get; init; } = [".txt"];

    public List<string> CsvExtensions { get; init; } = [".csv"];

    public bool DetectInputFileKindFromExtension { get; init; } = true;

    public bool TreatUnknownExtensionAsPlainText { get; init; } = false;

    public bool CaptureCsvSourceTitle { get; init; } = true;

    public bool CaptureCsvSourceRecordId { get; init; } = true;
}
