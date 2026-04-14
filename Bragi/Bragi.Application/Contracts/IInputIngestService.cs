using Bragi.Application.Configuration;
using Bragi.Domain.Enums;

namespace Bragi.Application.Contracts;

public interface IInputIngestService
{
    Task<InputFileKind> DetectInputFileKindAsync(
        string sourceFilePath,
        InputOptions inputOptions,
        CancellationToken cancellationToken = default);

    Task<string> ReadAllTextAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadCsvRowsAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default);
}
