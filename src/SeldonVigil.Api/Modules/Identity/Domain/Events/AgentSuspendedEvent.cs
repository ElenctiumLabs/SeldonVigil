using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Domain.Events;

internal sealed record AgentSuspendedEvent(
    AgentId AgentId,
    TenantId TenantId,
    string Reason,
    Guid SuspendedBy,
    DateTime EventTimestamp
) : BaseEvent;
