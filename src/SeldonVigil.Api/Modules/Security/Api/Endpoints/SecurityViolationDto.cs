using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record SecurityViolationDto(
    SecurityViolationId Id,
    SecurityPolicyId PolicyId,
    string PolicyName,
    AgentId AgentId,
    TraceId TraceId,
    string Severity,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt);
