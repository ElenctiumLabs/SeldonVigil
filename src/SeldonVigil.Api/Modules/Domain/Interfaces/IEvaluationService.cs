using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface IEvaluationService
{
    Task<EvaluationResult> EvaluateAsync<TRequest, TResponse>(
        string promptName,
        TRequest request,
        TResponse response,
        IReadOnlyList<ReasoningStep> reasoningSteps,
        CancellationToken ct = default);
}
