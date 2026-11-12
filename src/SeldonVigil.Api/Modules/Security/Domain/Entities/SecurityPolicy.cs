using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Events;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Security.Domain.Entities;

public sealed class SecurityPolicy : BaseAggregateRoot<SecurityPolicyId>
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PolicyCategory Category { get; private set; }
    public RuleDefinition RuleDefinition { get; private set; } = null!;
    public Severity Severity { get; private set; }
    public AutoResponse? AutoResponse { get; private set; }
    public PolicyStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }

    private SecurityPolicy() { }

    public static Result<SecurityPolicy> Create(
        TenantId tenantId,
        string name,
        string description,
        PolicyCategory category,
        RuleDefinition ruleDefinition,
        Severity severity,
        AutoResponse? autoResponse,
        Guid createdBy,
        IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return Result<SecurityPolicy>.Failure(Error.Validation(
                "Policy.InvalidName",
                "Policy name is required and must not exceed 100 characters."));
        }

        var policy = new SecurityPolicy
        {
            Id = SecurityPolicyId.New(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category,
            RuleDefinition = ruleDefinition,
            Severity = severity,
            AutoResponse = autoResponse,
            Status = PolicyStatus.Active,
            CreatedBy = createdBy,
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = dateTimeProvider.UtcNow
        };

        return Result<SecurityPolicy>.Success(policy);
    }

    public Result<Unit> Evaluate(Trace trace, IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        if (Status != PolicyStatus.Active)
        {
            return Result<Unit>.Success(Unit.Value);
        }

        var isMatch = RuleDefinition.Evaluate(trace);
        if (!isMatch)
        {
            return Result<Unit>.Success(Unit.Value);
        }

        _ = SecurityViolation.Create(
            Id,
            trace.TenantId,
            trace.AgentId,
            trace.Id,
            Severity,
            $"Policy '{Name}' violated by agent {trace.AgentId}",
            RuleDefinition,
            dateTimeProvider);

        AddDomainEvent(new PolicyViolatedEvent(
            Id,
            trace.TenantId,
            trace.AgentId,
            trace.Id,
            Severity,
            AutoResponse,
            dateTimeProvider.UtcNow));

        return Result<Unit>.Success(Unit.Value);
    }

    public void Disable(Guid disabledBy, IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        Status = PolicyStatus.Disabled;
        UpdatedAt = dateTimeProvider.UtcNow;
        AddDomainEvent(new PolicyDisabledEvent(Id, TenantId, disabledBy, dateTimeProvider.UtcNow));
    }
}
