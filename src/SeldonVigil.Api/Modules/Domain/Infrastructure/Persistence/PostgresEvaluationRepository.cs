using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence.Entities;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Domain.Services;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL persistence for prompt evaluation results (replaces the in-memory
/// store so evaluation history survives restarts).
/// </summary>
internal sealed class PostgresEvaluationRepository(SeldonVigilDbContext db) : IEvaluationRepository
{
    public async Task StoreAsync<TRequest, TResponse>(
        string promptName,
        TRequest request,
        TResponse response,
        EvaluationResult result,
        CancellationToken ct = default)
    {
        db.EvaluationResults.Add(new EvaluationResultEntity
        {
            Id = Guid.NewGuid(),
            PromptName = promptName,
            OverallScore = result.OverallScore,
            Passed = result.Passed,
            Metrics = [.. result.Metrics],
            EvaluationDurationMs = result.EvaluationDurationMs,
            EvaluatedAt = result.EvaluatedAt
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EvaluationResult>> GetRecentEvaluationsAsync(
        string promptName, int limit, CancellationToken ct = default)
    {
        var results = await db.EvaluationResults
            .Where(r => r.PromptName == promptName)
            .OrderByDescending(r => r.EvaluatedAt)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return results
            .Select(r => new EvaluationResult
            {
                OverallScore = r.OverallScore,
                Passed = r.Passed,
                Metrics = [.. r.Metrics],
                EvaluationDurationMs = r.EvaluationDurationMs,
                EvaluatedAt = r.EvaluatedAt
            })
            .ToList();
    }
}
