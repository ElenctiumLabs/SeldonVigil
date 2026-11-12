using SeldonVigil.Api.Modules.Security.Domain.Entities;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record ListSecurityPoliciesRequest(
    PolicyCategory? Category,
    PolicyStatus? Status,
    int Page = 1,
    int PageSize = 50);
