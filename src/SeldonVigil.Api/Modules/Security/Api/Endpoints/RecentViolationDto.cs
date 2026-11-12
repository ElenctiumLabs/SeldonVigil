using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record RecentViolationDto(
    SecurityViolationId Id,
    string AgentName,
    string PolicyName,
    string Severity,
    DateTime DetectedAt);
