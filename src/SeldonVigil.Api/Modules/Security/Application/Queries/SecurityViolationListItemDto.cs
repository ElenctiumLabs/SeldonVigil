using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed record SecurityViolationListItemDto(
    SecurityViolationId Id,
    SecurityPolicyId PolicyId,
    AgentId AgentId,
    string Severity,
    string Status,
    string Description,
    DateTime CreatedAt);
