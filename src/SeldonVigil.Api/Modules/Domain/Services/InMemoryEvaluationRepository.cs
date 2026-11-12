using SeldonVigil.Api.Modules.Domain.Interfaces;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// In-memory evaluation repository for development. Stores evaluation results in memory.
/// </summary>
internal sealed class InMemoryEvaluationRepository : IEvaluationRepository
{
    private readonly List<(string PromptName, EvaluationResult Result)> _results = new();

    public Task StoreAsync<TRequest, TResponse>(
        string promptName,
        TRequest request,
        TResponse response,
        EvaluationResult result,
        CancellationToken ct = default)
    {
        _results.Add((promptName, result));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EvaluationResult>> GetRecentEvaluationsAsync(
        string promptName, int limit, CancellationToken ct = default)
    {
        var results = _results
            .Where(r => r.PromptName == promptName)
            .OrderByDescending(r => r.Result.EvaluatedAt)
            .Take(limit)
            .Select(r => r.Result)
            .ToList();

        return Task.FromResult<IReadOnlyList<EvaluationResult>>(results);
    }
}
