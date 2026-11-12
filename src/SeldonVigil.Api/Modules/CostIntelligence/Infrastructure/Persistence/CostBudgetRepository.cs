using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence;

internal sealed class CostBudgetRepository(SeldonVigilDbContext dbContext)
    : ICostBudgetRepository
{
    public async Task AddAsync(CostBudget budget, CancellationToken ct = default)
    {
        await dbContext.CostBudgets.AddAsync(budget, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CostBudget>> GetApplicableBudgetsAsync(
        TenantId tenantId, AgentId agentId, CancellationToken ct = default)
    {
        var budgets = await dbContext.CostBudgets
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .Where(b =>
                b.ScopeType == BudgetScopeType.Tenant
                || (b.ScopeType == BudgetScopeType.Agent && b.ScopeId == agentId.Value))
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return budgets.AsReadOnly();
    }

    public async Task<IReadOnlyList<CostBudget>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var budgets = await dbContext.CostBudgets
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return budgets.AsReadOnly();
    }

    public async Task UpdateAsync(CostBudget budget, CancellationToken ct = default)
    {
        dbContext.CostBudgets.Update(budget);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<decimal>> GetRecentCostsAsync(
        AgentId agentId, TimeSpan window, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Subtract(window);
        var costs = await dbContext.CostBudgets
            .AsNoTracking()
            .Where(b => b.ScopeType == BudgetScopeType.Agent && b.ScopeId == agentId.Value)
            .Select(b => b.CurrentSpend.Amount)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return costs.AsReadOnly();
    }
}
