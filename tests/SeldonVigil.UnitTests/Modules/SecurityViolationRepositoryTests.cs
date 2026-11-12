using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.SharedKernel.Infrastructure;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Modules.Security.Infrastructure.Persistence;

public class SecurityViolationRepositoryTests : IDisposable
{
    private static readonly TenantId Tenant = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    private readonly SeldonVigilDbContext _dbContext;
    private readonly SecurityViolationRepository _repository;
    private readonly SystemDateTimeProvider _dateTimeProvider = new();

    public SecurityViolationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SeldonVigilDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new SeldonVigilDbContext(options, new FixedTenantContext(Tenant));
        _repository = new SecurityViolationRepository(_dbContext);
    }

    private sealed class FixedTenantContext(TenantId tenant) : SeldonVigil.Api.Shared.Infrastructure.Tenancy.ITenantContext
    {
        public Guid? TenantId => tenant.Value;
        public TenantId CurrentTenant => tenant;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task AddAsyncPersistsViolation()
    {
        var policyId = new SecurityPolicyId(Guid.NewGuid());
        var traceId = new TraceId(Guid.NewGuid());

        var violation = SecurityViolation.Create(
            policyId,
            Tenant,
            new AgentId(Guid.NewGuid()),
            traceId,
            Severity.High,
            "Test violation",
            new RuleDefinition { Type = RuleType.RegexMatch, Expression = "pattern", Parameters = new Dictionary<string, object> { { "pattern", ".*" } } },
            _dateTimeProvider);

        await _repository.AddAsync(violation);

        var exists = await _repository.ExistsForTraceAsync(policyId, traceId);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsForTraceAsyncIsScopedToPolicyAndTrace()
    {
        var policyId = new SecurityPolicyId(Guid.NewGuid());
        var traceId = new TraceId(Guid.NewGuid());

        var violation = SecurityViolation.Create(
            policyId,
            Tenant,
            new AgentId(Guid.NewGuid()),
            traceId,
            Severity.Medium,
            "Duplicate check",
            new RuleDefinition { Type = RuleType.RegexMatch, Expression = "pattern", Parameters = new Dictionary<string, object> { { "pattern", ".*" } } },
            _dateTimeProvider);

        await _repository.AddAsync(violation);

        (await _repository.ExistsForTraceAsync(policyId, traceId)).Should().BeTrue();
        (await _repository.ExistsForTraceAsync(new SecurityPolicyId(Guid.NewGuid()), traceId)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsForTraceAsyncIsFalseForUnknownPair()
    {
        (await _repository.ExistsForTraceAsync(
            new SecurityPolicyId(Guid.NewGuid()),
            new TraceId(Guid.NewGuid()))).Should().BeFalse();
    }
}
