using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Commands;

internal sealed record CreateCostBudgetCommand(
    string Name,
    BudgetScopeType ScopeType,
    Guid ScopeId,
    decimal Amount,
    string Currency,
    BudgetPeriod Period,
    List<decimal> AlertThresholds,
    EnforcementType EnforcementType
) : ICommand<CostBudgetId>;
