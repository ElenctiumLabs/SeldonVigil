using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Security.Infrastructure.Persistence;

internal sealed class SecurityViolationRepository(SeldonVigilDbContext dbContext) : ISecurityViolationRepository
{
    public async Task AddAsync(SecurityViolation violation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(violation);

        await dbContext.SecurityViolations.AddAsync(violation, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> ExistsForTraceAsync(
        SecurityPolicyId policyId,
        TraceId traceId,
        CancellationToken ct = default)
    {
        return await dbContext.SecurityViolations
            .AsNoTracking()
            .AnyAsync(v => v.PolicyId == policyId && v.TraceId == traceId, ct)
            .ConfigureAwait(false);
    }
    public async Task<IReadOnlyList<SecurityViolation>> GetByAgentAndDateRangeAsync(
        AgentId agentId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var violations = await dbContext.SecurityViolations
            .AsNoTracking()
            .Where(v => v.AgentId == agentId && v.CreatedAt >= from && v.CreatedAt <= to)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return violations.AsReadOnly();
    }

    public async Task<IReadOnlyList<SecurityViolation>> GetByTenantAndDateRangeAsync(
        TenantId tenantId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var violations = await dbContext.SecurityViolations
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.CreatedAt >= from && v.CreatedAt <= to)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return violations.AsReadOnly();
    }

    public async Task<IReadOnlyList<SecurityViolationTrendPoint>> GetDailyTrendAsync(
        TenantId tenantId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var trend = await dbContext.SecurityViolations
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId && DateOnly.FromDateTime(v.CreatedAt) >= DateOnly.FromDateTime(from) && DateOnly.FromDateTime(v.CreatedAt) <= DateOnly.FromDateTime(to))
            .GroupBy(v => v.CreatedAt.Date)
            .Select(g => new SecurityViolationTrendPoint(DateOnly.FromDateTime(g.Key), g.Count()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return trend.AsReadOnly();
    }
}
