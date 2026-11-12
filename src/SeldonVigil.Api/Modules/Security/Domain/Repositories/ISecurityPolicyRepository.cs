using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Domain.Repositories;

public interface ISecurityPolicyRepository
{
    Task AddAsync(SecurityPolicy policy, CancellationToken ct = default);
    Task<SecurityPolicy?> GetByIdAsync(SecurityPolicyId id, CancellationToken ct = default);
    Task<IReadOnlyList<SecurityPolicy>> GetByTenantAsync(TenantId tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SecurityPolicy>> GetActiveByTenantAsync(TenantId tenantId, CancellationToken ct = default);
    Task UpdateAsync(SecurityPolicy policy, CancellationToken ct = default);
}
