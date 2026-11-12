using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;

public interface ICostBudgetRepository
{
    Task AddAsync(CostBudget budget, CancellationToken ct = default);
    Task<IReadOnlyList<CostBudget>> GetApplicableBudgetsAsync(TenantId tenantId, AgentId agentId, CancellationToken ct = default);
    Task<IReadOnlyList<CostBudget>> GetAllActiveAsync(CancellationToken ct = default);
    Task UpdateAsync(CostBudget budget, CancellationToken ct = default);
    Task<IReadOnlyList<decimal>> GetRecentCostsAsync(AgentId agentId, TimeSpan window, CancellationToken ct = default);
}
