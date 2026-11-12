using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Domain.Events;

internal sealed record AgentRevokedEvent(
    AgentId AgentId,
    TenantId TenantId,
    string Reason,
    Guid RevokedBy,
    DateTime EventTimestamp
) : BaseEvent;
