using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Governance.Domain.Entities;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Governance.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL persistence for governance policies (replaces the in-memory store
/// so policies survive restarts). Tenants are filtered explicitly by parameter.
/// </summary>
internal sealed class GovernancePolicyRepository(SeldonVigilDbContext db)
{
    /// <summary>Persists immediately (sync convenience matching legacy call sites).</summary>
    public void Add(GovernancePolicy entity)
    {
        AddAsync(entity).GetAwaiter().GetResult();
    }

    /// <summary>Upserts and persists immediately.</summary>
    public async Task AddAsync(GovernancePolicy entity, CancellationToken ct = default)
    {
        var tracked = await db.GovernancePolicies.FindAsync([entity.Id], ct).ConfigureAwait(false);
        if (tracked is null)
        {
            db.GovernancePolicies.Add(entity);
        }
        else
        {
            db.Entry(tracked).CurrentValues.SetValues(entity);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Persists mutations made on a loaded entity.</summary>
    public Task SaveAsync(GovernancePolicy entity, CancellationToken ct = default)
        => AddAsync(entity, ct);

    public Task<GovernancePolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.GovernancePolicies.FirstOrDefaultAsync(
            p => p.Id == new SeldonVigil.Api.Modules.Governance.Domain.ValueObjects.GovernancePolicyId(id),
            cancellationToken);

    public async Task<IReadOnlyList<GovernancePolicy>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenantGuid = new SeldonVigil.Api.Modules.Identity.Domain.ValueObjects.TenantId(Guid.Parse(tenantId));
        var results = await db.GovernancePolicies
            .Where(p => p.TenantId == tenantGuid)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<GovernancePolicy>> GetActiveByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenantGuid = new SeldonVigil.Api.Modules.Identity.Domain.ValueObjects.TenantId(Guid.Parse(tenantId));
        var results = await db.GovernancePolicies
            .Where(p => p.TenantId == tenantGuid && p.Status == PolicyStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }
}

/// <summary>
/// PostgreSQL persistence for certification pipelines.
/// </summary>
internal sealed class CertificationPipelineRepository(SeldonVigilDbContext db)
{
    public async Task AddAsync(CertificationPipeline entity, CancellationToken ct = default)
    {
        var tracked = await db.CertificationPipelines.FindAsync([entity.Id], ct).ConfigureAwait(false);
        if (tracked is null)
        {
            db.CertificationPipelines.Add(entity);
        }
        else
        {
            db.Entry(tracked).CurrentValues.SetValues(entity);
            // Stages is a jsonb column with a value converter but no value comparer,
            // so EF change-tracking cannot detect the in-place mutation of the
            // PipelineStage list (e.g. ProgressStage flipping a stage to Passed).
            // In the worker/approve flow FindAsync returns the same tracked instance,
            // so assigning CurrentValue is a no-op. Force the column to be written.
            var stagesEntry = db.Entry(tracked).Property(p => p.Stages);
            stagesEntry.CurrentValue = entity.Stages;
            stagesEntry.IsModified = true;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task SaveAsync(CertificationPipeline entity, CancellationToken ct = default)
        => AddAsync(entity, ct);

    public Task<CertificationPipeline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.CertificationPipelines.FirstOrDefaultAsync(
            p => p.Id == new SeldonVigil.Api.Modules.Governance.Domain.ValueObjects.CertificationPipelineId(id),
            cancellationToken);

    public async Task<IReadOnlyList<CertificationPipeline>> GetByAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        var agentGuid = new SeldonVigil.Api.Modules.Identity.Domain.ValueObjects.AgentId(Guid.Parse(agentId));
        var results = await db.CertificationPipelines
            .Where(p => p.AgentId == agentGuid)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<CertificationPipeline>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        // CertificationPipeline has no global tenant query filter (unlike Agent/Security),
        // so this cross-tenant poll is safe. Callers set TenantScope per pipeline before
        // touching tenant-filtered repositories.
        var results = await db.CertificationPipelines
            .Where(p => p.Status == PipelineStatus.Pending || p.Status == PipelineStatus.InProgress)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<CertificationPipeline>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenantGuid = new SeldonVigil.Api.Modules.Identity.Domain.ValueObjects.TenantId(Guid.Parse(tenantId));
        var results = await db.CertificationPipelines
            .Where(p => p.TenantId == tenantGuid)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }
}
