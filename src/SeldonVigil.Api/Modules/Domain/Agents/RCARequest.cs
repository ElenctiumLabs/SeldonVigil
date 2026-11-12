using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Domain.Agents;

internal sealed record RCARequest(
    string AnomalyType,
    AgentId AgentId,
    TraceId? TraceId,
    DateTime AnomalyStartTime,
    DateTime AnomalyEndTime);
