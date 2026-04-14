using Bragi.Application.Configuration;
using Bragi.Domain.Results;

namespace Bragi.Application.Contracts;

public interface ISubjectExtractionService
{
    Task<ExtractionResult> ExtractFromPlainTextAsync(
        string sourceFilePath,
        string textContent,
        InputOptions inputOptions,
        BehaviorOptions behaviorOptions,
        CancellationToken cancellationToken = default);

    Task<ExtractionResult> ExtractFromCsvAsync(
        string sourceFilePath,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows,
        CsvColumns csvColumns,
        InputOptions inputOptions,
        BehaviorOptions behaviorOptions,
        CancellationToken cancellationToken = default);
}
