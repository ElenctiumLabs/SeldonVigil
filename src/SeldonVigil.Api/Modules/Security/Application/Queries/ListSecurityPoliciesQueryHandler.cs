using SeldonVigil.Api.Modules.Identity.Application.Queries;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed class ListSecurityPoliciesQueryHandler(
    ISecurityPolicyRepository policyRepository,
    IRequestContext requestContext)
    : IQueryHandler<ListSecurityPoliciesQuery, PaginatedResult<SecurityPolicyListItemDto>>
{
    public async Task<Shared.Domain.Result<PaginatedResult<SecurityPolicyListItemDto>>> Handle(
        ListSecurityPoliciesQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return Shared.Domain.Result<PaginatedResult<SecurityPolicyListItemDto>>.Failure(Error.Unauthorized(
                "Tenant.Required", "Tenant context is required."));
        }

        var tenantId = new TenantId(Guid.Parse(requestContext.TenantId));

        var policies = await policyRepository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false);

        var filteredPolicies = policies.AsEnumerable();

        if (request.Category.HasValue)
        {
            filteredPolicies = filteredPolicies.Where(p => p.Category == request.Category.Value);
        }

        if (request.Status.HasValue)
        {
            filteredPolicies = filteredPolicies.Where(p => p.Status == request.Status.Value);
        }

        var totalCount = filteredPolicies.Count();
        var pagedItems = filteredPolicies
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new SecurityPolicyListItemDto(
                p.Id,
                p.Name,
                p.Description,
                p.Category.ToString(),
                p.Status.ToString(),
                p.Severity.ToString(),
                p.CreatedAt))
            .ToList();

        var result = new PaginatedResult<SecurityPolicyListItemDto>(
            pagedItems,
            totalCount,
            request.Page,
            request.PageSize);

        return Result.Success(result);
    }
}
