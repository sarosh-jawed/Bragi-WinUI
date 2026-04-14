using Bragi.Application.Configuration;
using Bragi.Domain.Results;

namespace Bragi.Application.Contracts;

public interface ICategorizationService
{
    Task<CategorizationResult> CategorizeAsync(
        ExtractionResult extractionResult,
        IReadOnlyList<CategoryRule> categoryRules,
        BehaviorOptions behaviorOptions,
        CancellationToken cancellationToken = default);
}
