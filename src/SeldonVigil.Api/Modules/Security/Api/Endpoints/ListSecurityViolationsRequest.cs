using SeldonVigil.Api.Modules.Security.Domain.Entities;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record ListSecurityViolationsRequest(
    Guid? AgentId,
    Severity? Severity,
    ViolationStatus? Status,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
