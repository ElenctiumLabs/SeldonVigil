using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Domain.Events;

internal sealed record AgentRegisteredEvent(
    AgentId AgentId,
    TenantId TenantId,
    string ExternalId,
    AgentFramework Framework,
    ModelProvider ModelProvider,
    DateTime EventTimestamp
) : BaseEvent;
