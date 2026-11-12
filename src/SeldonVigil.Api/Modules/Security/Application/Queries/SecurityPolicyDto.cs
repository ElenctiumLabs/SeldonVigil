using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed record SecurityPolicyDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    RuleDefinition RuleDefinition,
    string Severity,
    string? AutoResponse,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
