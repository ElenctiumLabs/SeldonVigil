using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record SecurityPolicyListItemDto(
    SecurityPolicyId Id,
    string Name,
    string Category,
    string Severity,
    string Status,
    DateTime CreatedAt);
