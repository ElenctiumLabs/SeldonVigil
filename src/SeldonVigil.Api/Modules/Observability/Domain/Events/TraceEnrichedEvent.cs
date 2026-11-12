using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Observability.Domain.Events;

public sealed record TraceEnrichedEvent(
    TraceId TraceId,
    AgentId AgentId,
    decimal Cost,
    int TokenUsage) : BaseEvent;
