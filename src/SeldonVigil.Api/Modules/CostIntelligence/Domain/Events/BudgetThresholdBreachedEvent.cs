using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.CostIntelligence.Domain.Events;

internal sealed record BudgetThresholdBreachedEvent(
    CostBudgetId BudgetId,
    TenantId TenantId,
    BudgetScopeType ScopeType,
    Guid ScopeId,
    Money BudgetAmount,
    Money CurrentSpend,
    decimal BreachedThreshold,
    DateTime EventTimestamp
) : BaseEvent;
