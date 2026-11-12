using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Security.Domain.Entities;

internal sealed class SecurityViolation : BaseEntity<SecurityViolationId>
{
    public SecurityPolicyId PolicyId { get; private set; }
    public TenantId TenantId { get; private set; }
    public AgentId AgentId { get; private set; }
    public TraceId TraceId { get; private set; }
    public Severity Severity { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public RuleDefinition TriggeringRule { get; private set; } = null!;
    public ViolationStatus Status { get; private set; }
    public DateTime? ResolvedAt { get; }
    public Guid? ResolvedBy { get; }
    public string? ResolutionNotes { get; }

    private SecurityViolation() { }

    public static SecurityViolation Create(
        SecurityPolicyId policyId,
        TenantId tenantId,
        AgentId agentId,
        TraceId traceId,
        Severity severity,
        string description,
        RuleDefinition triggeringRule,
        IDateTimeProvider dateTimeProvider)
    {
        return new SecurityViolation
        {
            Id = SecurityViolationId.New(),
            PolicyId = policyId,
            TenantId = tenantId,
            AgentId = agentId,
            TraceId = traceId,
            Severity = severity,
            Description = description,
            TriggeringRule = triggeringRule,
            Status = ViolationStatus.Open,
            CreatedAt = dateTimeProvider.UtcNow
        };
    }
}
