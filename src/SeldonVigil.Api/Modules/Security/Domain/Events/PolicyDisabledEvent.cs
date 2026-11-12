using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Security.Domain.Events;

internal sealed record PolicyDisabledEvent(
    SecurityPolicyId PolicyId,
    TenantId TenantId,
    Guid DisabledBy,
    DateTime EventTimestamp) : BaseEvent;
