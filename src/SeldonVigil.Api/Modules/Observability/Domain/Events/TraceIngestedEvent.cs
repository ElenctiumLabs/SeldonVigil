using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Observability.Domain.Events;

public sealed record TraceIngestedEvent(
    TraceId TraceId,
    AgentId AgentId,
    TenantId TenantId,
    Dictionary<string, object> Metadata,
    decimal Cost = 0,
    int TokenUsage = 0) : BaseEvent;
