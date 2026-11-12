using SeldonVigil.Api.Modules.Governance.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Governance.Domain.Events;

internal sealed record GovernancePolicyVersionedEvent(
    GovernancePolicyId PreviousPolicyId,
    GovernancePolicyId NewPolicyId,
    TenantId TenantId,
    int NewVersion,
    DateTime EventTimestamp) : BaseEvent;
