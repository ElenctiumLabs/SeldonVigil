using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.Events;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Domain.Entities;

internal sealed class Agent : BaseAggregateRoot<AgentId>
{
    public TenantId TenantId { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; }
    public AgentFramework Framework { get; private set; }
    public string? FrameworkVersion { get; }
    public ModelProvider ModelProvider { get; private set; }
    public string ModelName { get; private set; } = string.Empty;
    public string? ModelVersion { get; }
    public AgentStatus Status { get; private set; }
    public CapabilityManifest CapabilityManifest { get; private set; } = null!;
    public string CapabilityManifestHash { get; private set; } = string.Empty;

    // Identity
    public Guid? IdentityTokenJti { get; private set; }
    public DateTime? IdentityTokenIssuedAt { get; private set; }
    public DateTime? IdentityTokenExpiresAt { get; private set; }

    // Version control
    public int SourceVersion { get; }
    public string? SourceCodeHash { get; }
    public string? PromptTemplateHash { get; }

    // Lifecycle timestamps
    public Guid CreatedBy { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public string? SuspendedReason { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public DateTime? ArchivedAt { get; }
    public DateTime? DeletedAt { get; }

    private Agent() { } // EF Core

    public static Result<Agent> Create(
        TenantId tenantId,
        string externalId,
        string name,
        AgentFramework framework,
        ModelProvider modelProvider,
        string modelName,
        CapabilityManifest manifest,
        Guid createdBy,
        IDateTimeProvider dateTimeProvider)
    {
        var validation = Validate(externalId, name, modelName);
        if (validation.IsFailure)
        {
            return Result<Agent>.Failure(validation.Error!);
        }

        var agent = new Agent
        {
            Id = AgentId.New(),
            TenantId = tenantId,
            ExternalId = externalId.Trim(),
            Name = name.Trim(),
            Framework = framework,
            ModelProvider = modelProvider,
            ModelName = modelName.Trim(),
            Status = AgentStatus.Draft,
            CapabilityManifest = manifest,
            CapabilityManifestHash = manifest.ComputeHash(),
            CreatedBy = createdBy,
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = dateTimeProvider.UtcNow
        };

        agent.AddDomainEvent(new AgentRegisteredEvent(
            agent.Id,
            tenantId,
            externalId,
            framework,
            modelProvider,
            dateTimeProvider.UtcNow));

        return Result<Agent>.Success(agent);
    }

    public Result<Unit> Activate(Guid activatedBy, IDateTimeProvider dateTimeProvider)
    {
        if (Status != AgentStatus.Draft && Status != AgentStatus.PendingCertification)
        {
            return Result<Unit>.Failure(Error.Conflict(
                "Agent.InvalidStatus",
                $"Cannot activate agent in status '{Status}'. Expected 'Draft' or 'PendingCertification'."));
        }

        Status = AgentStatus.Active;
        ActivatedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;
        IncrementVersion();

        AddDomainEvent(new AgentActivatedEvent(Id, TenantId, activatedBy, dateTimeProvider.UtcNow));
        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> Suspend(string reason, Guid suspendedBy, IDateTimeProvider dateTimeProvider)
    {
        if (Status != AgentStatus.Active)
        {
            return Result<Unit>.Failure(Error.Conflict(
                "Agent.InvalidStatus",
                $"Cannot suspend agent in status '{Status}'. Expected 'Active'."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result<Unit>.Failure(Error.Validation(
                "Agent.SuspendReasonRequired",
                "Suspension reason is required."));
        }

        Status = AgentStatus.Suspended;
        SuspendedAt = dateTimeProvider.UtcNow;
        SuspendedReason = reason.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;
        IncrementVersion();

        // Revoke all active tokens
        IdentityTokenJti = null;
        IdentityTokenExpiresAt = null;

        AddDomainEvent(new AgentSuspendedEvent(Id, TenantId, reason, suspendedBy, dateTimeProvider.UtcNow));
        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> Revoke(string reason, Guid revokedBy, IDateTimeProvider dateTimeProvider)
    {
        if (Status == AgentStatus.Revoked)
        {
            return Result<Unit>.Failure(Error.Conflict(
                "Agent.AlreadyRevoked",
                "Agent is already revoked."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result<Unit>.Failure(Error.Validation(
                "Agent.RevokeReasonRequired",
                "Revocation reason is required."));
        }

        Status = AgentStatus.Revoked;
        RevokedAt = dateTimeProvider.UtcNow;
        RevokedReason = reason.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;
        IncrementVersion();

        IdentityTokenJti = null;
        IdentityTokenExpiresAt = null;

        AddDomainEvent(new AgentRevokedEvent(Id, TenantId, reason, revokedBy, dateTimeProvider.UtcNow));
        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> UpdateManifest(CapabilityManifest newManifest, IDateTimeProvider dateTimeProvider)
    {
        var newHash = newManifest.ComputeHash();
        if (CapabilityManifestHash == newHash)
        {
            return Result<Unit>.Success(Unit.Value); // No change
        }

        CapabilityManifest = newManifest;
        CapabilityManifestHash = newHash;
        UpdatedAt = dateTimeProvider.UtcNow;
        IncrementVersion();

        // Status reverts to draft for re-certification
        if (Status == AgentStatus.Active)
        {
            Status = AgentStatus.Draft;
            AddDomainEvent(new AgentManifestChangedEvent(Id, TenantId, newHash, dateTimeProvider.UtcNow));
        }

        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> IssueIdentityToken(Guid jti, DateTime expiresAt, IDateTimeProvider dateTimeProvider)
    {
        if (Status != AgentStatus.Active)
        {
            return Result<Unit>.Failure(Error.Conflict(
                "Agent.NotActive",
                $"Cannot issue identity token for agent in status '{Status}'."));
        }

        IdentityTokenJti = jti;
        IdentityTokenIssuedAt = dateTimeProvider.UtcNow;
        IdentityTokenExpiresAt = expiresAt;
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result<Unit>.Success(Unit.Value);
    }

    private static Result<Unit> Validate(string externalId, string name, string modelName)
    {
        if (string.IsNullOrWhiteSpace(externalId) || externalId.Length > 255)
        {
            return Result<Unit>.Failure(Error.Validation(
                "Agent.InvalidExternalId",
                "External ID is required and must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            return Result<Unit>.Failure(Error.Validation(
                "Agent.InvalidName",
                "Name is required and must not exceed 255 characters."));
        }

        if (string.IsNullOrWhiteSpace(modelName) || modelName.Length > 100)
        {
            return Result<Unit>.Failure(Error.Validation(
                "Agent.InvalidModelName",
                "Model name is required and must not exceed 100 characters."));
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
