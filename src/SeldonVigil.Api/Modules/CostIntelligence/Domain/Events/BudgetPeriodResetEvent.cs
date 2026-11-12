using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.CostIntelligence.Domain.Events;

public sealed record BudgetPeriodResetEvent(
    CostBudgetId BudgetId,
    TenantId TenantId,
    BudgetScopeType ScopeType,
    Guid ScopeId,
    Money BudgetAmount,
    DateTime EventTimestamp
) : BaseEvent;
