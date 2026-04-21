using Bragi.Application.Configuration;
using Bragi.Domain.Results;

namespace Bragi.Application.Contracts;

public interface ITextExportService
{
    Task<ExportResult> ExportAsync(
        CategorizationResult categorizationResult,
        DateTimeOffset exportTimestampUtc,
        Output outputOptions,
        TextTemplate textTemplate,
        IReadOnlyList<CategoryRule> categoryRules,
        CancellationToken cancellationToken = default);
}
