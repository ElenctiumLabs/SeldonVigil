using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Governance.Domain.Events;
using SeldonVigil.Api.Modules.Governance.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Governance.Domain.Entities;

internal sealed class GovernancePolicy : BaseAggregateRoot<GovernancePolicyId>
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PolicyCategory Category { get; private set; }
    public string DefinitionYaml { get; private set; } = string.Empty;
    public PolicyStatus Status { get; private set; }
    public string? PreviousVersionId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? DeprecatedAt { get; private set; }

    private GovernancePolicy() { }

    public static Result<GovernancePolicy> Create(
        TenantId tenantId,
        string name,
        string description,
        PolicyCategory category,
        string definitionYaml,
        Guid createdBy,
        IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            return Result<GovernancePolicy>.Failure(Error.Validation(
                "Policy.InvalidName", "Policy name is required and must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(definitionYaml))
        {
            return Result<GovernancePolicy>.Failure(Error.Validation(
                "Policy.InvalidDefinition", "Policy definition is required."));
        }

        if (definitionYaml.Length < 5)
        {
            return Result<GovernancePolicy>.Failure(Error.Validation(
                "Policy.InvalidDefinition", "Policy definition is too short."));
        }

        var policy = new GovernancePolicy
        {
            Id = GovernancePolicyId.New(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category,
            DefinitionYaml = definitionYaml,
            Version = 1,
            Status = PolicyStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = dateTimeProvider.UtcNow
        };

        return Result<GovernancePolicy>.Success(policy);
    }

    public Result<Unit> Activate(Guid activatedBy, IDateTimeProvider dateTimeProvider)
    {
        if (Status != PolicyStatus.Draft)
        {
            return Result<Unit>.Failure(Error.Conflict(
                "Policy.InvalidStatus", $"Cannot activate policy in status '{Status}'."));
        }

        Status = PolicyStatus.Active;
        ActivatedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new GovernancePolicyActivatedEvent(
            Id, TenantId, Version, activatedBy, dateTimeProvider.UtcNow));

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<GovernancePolicy> CreateNewVersion(
        string newDefinitionYaml,
        Guid createdBy,
        IDateTimeProvider dateTimeProvider)
    {
        if (Status != PolicyStatus.Active)
        {
            return Result<GovernancePolicy>.Failure(Error.Conflict(
                "Policy.NotActive", "Only active policies can be versioned."));
        }

        var newPolicyResult = Create(TenantId, Name, Description, Category, newDefinitionYaml, createdBy, dateTimeProvider);
        if (newPolicyResult.IsFailure)
        {
            return Result<GovernancePolicy>.Failure(newPolicyResult.Error!);
        }

        var newPolicy = newPolicyResult.Value!;
        newPolicy.Version = Version + 1;
        newPolicy.PreviousVersionId = Id.ToString();

        Status = PolicyStatus.Deprecated;
        DeprecatedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;

        AddDomainEvent(new GovernancePolicyVersionedEvent(
            Id, newPolicy.Id, TenantId, Version + 1, dateTimeProvider.UtcNow));

        return Result<GovernancePolicy>.Success(newPolicy);
    }
}

// YAML Schema for policy definitions
