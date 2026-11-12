using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Security.Domain.Events;

public sealed record PolicyViolatedEvent(
    SecurityPolicyId PolicyId,
    TenantId TenantId,
    AgentId AgentId,
    TraceId TraceId,
    Severity Severity,
    AutoResponse? AutoResponse,
    DateTime EventTimestamp) : BaseEvent;
