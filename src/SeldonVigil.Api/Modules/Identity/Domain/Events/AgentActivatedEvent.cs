using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Domain.Events;

internal sealed record AgentActivatedEvent(
    AgentId AgentId,
    TenantId TenantId,
    Guid ActivatedBy,
    DateTime EventTimestamp
) : BaseEvent;
