using Bragi.Application.Configuration;
using Bragi.Domain.Results;

namespace Bragi.Application.Contracts;

public interface ITextExportService
{
    Task ExportAsync(
        CategorizationResult categorizationResult,
        RunSummary runSummary,
        Output outputOptions,
        TextTemplate textTemplate,
        IReadOnlyList<CategoryRule> categoryRules,
        CancellationToken cancellationToken = default);
}
