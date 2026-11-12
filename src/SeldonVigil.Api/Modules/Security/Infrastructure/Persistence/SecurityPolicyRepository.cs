using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Security.Infrastructure.Persistence;

internal sealed class SecurityPolicyRepository(SeldonVigilDbContext dbContext) : ISecurityPolicyRepository
{
    public async Task AddAsync(SecurityPolicy policy, CancellationToken ct = default)
    {
        await dbContext.SecurityPolicies.AddAsync(policy, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<SecurityPolicy?> GetByIdAsync(SecurityPolicyId id, CancellationToken ct = default)
    {
        return await dbContext.SecurityPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SecurityPolicy>> GetByTenantAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var policies = await dbContext.SecurityPolicies
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return policies.AsReadOnly();
    }

    public async Task<IReadOnlyList<SecurityPolicy>> GetActiveByTenantAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var policies = await dbContext.SecurityPolicies
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == PolicyStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return policies.AsReadOnly();
    }

    public async Task UpdateAsync(SecurityPolicy policy, CancellationToken ct = default)
    {
        dbContext.SecurityPolicies.Update(policy);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
