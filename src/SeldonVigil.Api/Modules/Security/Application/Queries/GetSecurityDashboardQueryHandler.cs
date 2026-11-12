using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Api.Endpoints;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed class GetSecurityDashboardQueryHandler(
    ISecurityPolicyRepository policyRepository,
    ISecurityViolationRepository violationRepository,
    IDateTimeProvider dateTimeProvider,
    IRequestContext requestContext)
    : IQueryHandler<GetSecurityDashboardQuery, SecurityDashboardDto>
{
    public async Task<Shared.Domain.Result<SecurityDashboardDto>> Handle(
        GetSecurityDashboardQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return Shared.Domain.Result<SecurityDashboardDto>.Failure(Error.Unauthorized(
                "Tenant.Required", "Tenant context is required."));
        }

        var tenantId = new TenantId(Guid.Parse(requestContext.TenantId));
        var now = dateTimeProvider.UtcNow;
        var yesterday = now.AddDays(-1);
        var sevenDaysAgo = now.AddDays(-7);

        var policies = await policyRepository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false);
        var activePolicies = policies.Where(p => p.Status == PolicyStatus.Active).ToList();

        var violations24h = await violationRepository.GetByTenantAndDateRangeAsync(
            tenantId, yesterday, now, ct).ConfigureAwait(false);

        var openViolations = violations24h.Where(v => v.Status == ViolationStatus.Open).ToList();
        var criticalViolations = violations24h.Where(v => v.Severity == Severity.Critical).ToList();

        var trend = await violationRepository.GetDailyTrendAsync(
            tenantId, sevenDaysAgo, now, ct).ConfigureAwait(false);

        var recent = violations24h
            .OrderByDescending(v => v.CreatedAt)
            .Take(10)
            .Select(v => new RecentViolationDto(
                v.Id,
                "Agent-" + v.AgentId.ToString()[..8], // Would lookup actual name
                "Policy-" + v.PolicyId.ToString()[..8],
                v.Severity.ToString(),
                v.CreatedAt))
            .ToList();

        var dashboard = new SecurityDashboardDto(
            policies.Count,
            activePolicies.Count,
            violations24h.Count,
            openViolations.Count,
            criticalViolations.Count,
            recent,
            trend.Select(t => new TrendPointDto(t.Date, t.Count)).ToList());

        return Result.Success(dashboard);
    }
}
