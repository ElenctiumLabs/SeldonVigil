using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.AIReasoning.Application.Commands;

internal sealed record GenerateRootCauseAnalysisCommand(
    TenantId TenantId,
    string AnomalyType,
    AgentId AgentId,
    TraceId? TraceId,
    DateTime AnomalyStartTime,
    DateTime AnomalyEndTime
) : ICommand<RootCauseAnalysisResult>;
