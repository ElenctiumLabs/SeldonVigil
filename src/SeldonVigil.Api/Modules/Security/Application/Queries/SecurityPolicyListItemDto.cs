using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed record SecurityPolicyListItemDto(
    SecurityPolicyId Id,
    string Name,
    string Description,
    string Category,
    RuleDefinition RuleDefinition,
    string Status,
    string Severity,
    string? AutoResponse,
    DateTime CreatedAt,
    DateTime UpdatedAt);
