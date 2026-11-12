using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.AIReasoning.Domain.Entities;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.AIReasoning.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL persistence for root-cause analyses (replaces the in-memory
/// store so RCA history survives restarts). Soft-delete aware.
/// </summary>
internal sealed class RootCauseAnalysisRepository(SeldonVigilDbContext db)
{
    public async Task AddAsync(RootCauseAnalysis entity, CancellationToken ct = default)
    {
        var tracked = await db.RootCauseAnalyses.FindAsync([entity.Id], ct).ConfigureAwait(false);
        if (tracked is null)
        {
            db.RootCauseAnalyses.Add(entity);
        }
        else
        {
            db.Entry(tracked).CurrentValues.SetValues(entity);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<RootCauseAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.RootCauseAnalyses.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<RootCauseAnalysis>> GetByAgentAsync(string agentId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var results = await db.RootCauseAnalyses
            .Where(r => r.AgentId == agentId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<RootCauseAnalysis>> GetByTenantAsync(string tenantId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var results = await db.RootCauseAnalyses
            .Where(r => r.TenantId == tenantId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }
}
