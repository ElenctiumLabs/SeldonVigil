using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Commands;

internal sealed record CostAnomalyDetectedEvent(
    TraceId TraceId,
    AgentId AgentId,
    decimal ExpectedCost,
    decimal ActualCost,
    decimal DeviationPercent,
    DateTime EventTimestamp
) : BaseEvent;
