using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface IEvaluationRepository
{
    Task StoreAsync<TRequest, TResponse>(
        string promptName,
        TRequest request,
        TResponse response,
        EvaluationResult result,
        CancellationToken ct = default);

    Task<IReadOnlyList<EvaluationResult>> GetRecentEvaluationsAsync(
        string promptName,
        int limit,
        CancellationToken ct = default);
}
