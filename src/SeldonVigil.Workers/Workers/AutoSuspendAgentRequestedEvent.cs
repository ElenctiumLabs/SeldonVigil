using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Workers.Workers;

internal sealed record AutoSuspendAgentRequestedEvent(
    AgentId AgentId,
    SecurityPolicyId PolicyId,
    string Reason
) : BaseEvent;
