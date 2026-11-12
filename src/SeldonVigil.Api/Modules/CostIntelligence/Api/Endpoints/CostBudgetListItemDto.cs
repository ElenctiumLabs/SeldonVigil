using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;

internal sealed record CostBudgetListItemDto(
    CostBudgetId Id,
    string Name,
    string ScopeType,
    Guid ScopeId,
    Money BudgetAmount,
    Money CurrentSpend,
    decimal UtilizationPercent,
    string Status);
