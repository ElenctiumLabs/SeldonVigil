using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;

namespace SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;

internal sealed record CreateCostBudgetRequest(
    string Name,
    BudgetScopeType ScopeType,
    Guid ScopeId,
    decimal Amount,
    string Currency,
    BudgetPeriod Period,
    List<decimal> AlertThresholds,
    EnforcementType EnforcementType);
