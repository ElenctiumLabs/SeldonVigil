using SeldonVigil.Api.Modules.Governance.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Governance.Domain.Events;

internal sealed record GovernancePolicyActivatedEvent(
    GovernancePolicyId PolicyId,
    TenantId TenantId,
    int Version,
    Guid ActivatedBy,
    DateTime EventTimestamp) : BaseEvent;
