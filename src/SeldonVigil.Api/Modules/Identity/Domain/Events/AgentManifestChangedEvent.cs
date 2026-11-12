using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Domain.Events;

internal sealed record AgentManifestChangedEvent(
    AgentId AgentId,
    TenantId TenantId,
    string NewManifestHash,
    DateTime EventTimestamp
) : BaseEvent;
