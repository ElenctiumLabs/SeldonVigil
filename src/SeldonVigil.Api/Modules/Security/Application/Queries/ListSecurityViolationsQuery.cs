using SeldonVigil.Api.Modules.Identity.Application.Queries;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed record ListSecurityViolationsQuery(
    Guid? AgentId,
    Severity? Severity,
    ViolationStatus? Status,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<SecurityViolationListItemDto>>;
