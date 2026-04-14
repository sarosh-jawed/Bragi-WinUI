using Bragi.Domain.Results;

namespace Bragi.Application.Contracts;

public interface IWorkflowOrchestrator
{
    Task<ExtractionResult> PreviewExtractionAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default);

    Task<CategorizationResult> PreviewCategorizationAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default);

    Task<RunSummary> ExecuteAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default);
}
