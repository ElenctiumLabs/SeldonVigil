using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Application.Queries;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed class ListSecurityViolationsQueryHandler(
    ISecurityViolationRepository violationRepository,
    IRequestContext requestContext)
    : IQueryHandler<ListSecurityViolationsQuery, PaginatedResult<SecurityViolationListItemDto>>
{
    public async Task<Shared.Domain.Result<PaginatedResult<SecurityViolationListItemDto>>> Handle(
        ListSecurityViolationsQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return Shared.Domain.Result<PaginatedResult<SecurityViolationListItemDto>>.Failure(Error.Unauthorized(
                "Tenant.Required", "Tenant context is required."));
        }

        var tenantId = new TenantId(Guid.Parse(requestContext.TenantId));
        var from = request.From ?? DateTime.UtcNow.AddDays(-7);
        var to = request.To ?? DateTime.UtcNow;

        var violations = await violationRepository.GetByTenantAndDateRangeAsync(tenantId, from, to, ct)
            .ConfigureAwait(false);

        var filteredViolations = violations.AsEnumerable();

        if (request.AgentId.HasValue)
        {
            filteredViolations = filteredViolations.Where(v => v.AgentId == new AgentId(request.AgentId.Value));
        }

        if (request.Severity.HasValue)
        {
            filteredViolations = filteredViolations.Where(v => v.Severity == request.Severity.Value);
        }

        if (request.Status.HasValue)
        {
            filteredViolations = filteredViolations.Where(v => v.Status == request.Status.Value);
        }

        var totalCount = filteredViolations.Count();
        var pagedItems = filteredViolations
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new SecurityViolationListItemDto(
                v.Id,
                v.PolicyId,
                v.AgentId,
                v.Severity.ToString(),
                v.Status.ToString(),
                v.Description,
                v.CreatedAt))
            .ToList();

        var result = new PaginatedResult<SecurityViolationListItemDto>(
            pagedItems,
            totalCount,
            request.Page,
            request.PageSize);

        return Result.Success(result);
    }
}
