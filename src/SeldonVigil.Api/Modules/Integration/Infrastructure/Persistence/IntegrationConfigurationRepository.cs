using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Integration.Domain.Entities;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Integration.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL persistence for integration configurations (replaces the
/// in-memory store so integrations survive restarts). Soft-delete aware.
/// </summary>
internal sealed class IntegrationConfigurationRepository(SeldonVigilDbContext db)
{
    public async Task AddAsync(IntegrationConfiguration entity, CancellationToken ct = default)
    {
        var tracked = await db.Integrations.FindAsync([entity.Id], ct).ConfigureAwait(false);
        if (tracked is null)
        {
            db.Integrations.Add(entity);
        }
        else
        {
            db.Entry(tracked).CurrentValues.SetValues(entity);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Persists mutations made on a loaded entity.</summary>
    public Task UpdateAsync(IntegrationConfiguration entity, CancellationToken ct = default)
    {
        entity.MarkAsUpdated();
        return db.SaveChangesAsync(ct);
    }

    public Task<IntegrationConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Integrations.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<IntegrationConfiguration>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var results = await db.Integrations
            .Where(i => i.TenantId == tenantId && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<IntegrationConfiguration>> GetByTenantAndTypeAsync(string tenantId, IntegrationType type, CancellationToken cancellationToken = default)
    {
        var results = await db.Integrations
            .Where(i => i.TenantId == tenantId && i.Type == type && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }
}
