using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Domain.Repositories;

internal interface IAgentRepository
{
    ValueTask<Agent?> GetByIdAsync(AgentId id, CancellationToken ct = default);
    ValueTask<Agent?> GetByExternalIdAsync(TenantId tenantId, string externalId, CancellationToken ct = default);
    ValueTask<IReadOnlyList<Agent>> GetByTenantAsync(TenantId tenantId, AgentStatus? status = null, CancellationToken ct = default);
    ValueTask<IReadOnlyList<Agent>> GetByFrameworkAsync(TenantId tenantId, AgentFramework framework, CancellationToken ct = default);
    ValueTask<bool> ExistsAsync(TenantId tenantId, string externalId, CancellationToken ct = default);
    Task AddAsync(Agent agent, CancellationToken ct = default);
    Task UpdateAsync(Agent agent, CancellationToken ct = default);
    Task DeleteAsync(AgentId id, CancellationToken ct = default);
}
