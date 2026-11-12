using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Domain.Repositories;

internal interface ISecurityViolationRepository
{
    Task AddAsync(SecurityViolation violation, CancellationToken ct = default);
    Task<bool> ExistsForTraceAsync(SecurityPolicyId policyId, TraceId traceId, CancellationToken ct = default);
    Task<IReadOnlyList<SecurityViolation>> GetByAgentAndDateRangeAsync(AgentId agentId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<SecurityViolation>> GetByTenantAndDateRangeAsync(TenantId tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<SecurityViolationTrendPoint>> GetDailyTrendAsync(TenantId tenantId, DateTime from, DateTime to, CancellationToken ct = default);
}
