using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Governance.Domain.Events;
using SeldonVigil.Api.Modules.Governance.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Governance.Domain.Entities;

internal sealed class CertificationPipeline : BaseAggregateRoot<CertificationPipelineId>
{
    public TenantId TenantId { get; private set; }
    public AgentId AgentId { get; private set; }
    public PipelineStatus Status { get; private set; }
    public List<PipelineStage> Stages { get; private set; } = [];
    public string? CurrentStage { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public string? ApprovalNotes { get; private set; }

    private CertificationPipeline() { }

    public static CertificationPipeline Create(
        TenantId tenantId,
        AgentId agentId,
        Guid createdBy,
        IDateTimeProvider dateTimeProvider)
    {
        return new CertificationPipeline
        {
            Id = CertificationPipelineId.New(),
            TenantId = tenantId,
            AgentId = agentId,
            Status = PipelineStatus.Pending,
            Stages =
            [
                new("static_analysis", "Static code analysis", true),
                new("security_scan", "Security vulnerability scan", true),
                new("policy_validation", "Policy compliance validation", true),
                new("manual_approval", "Manual review and approval", false)
            ],
            CurrentStage = "static_analysis",
            StartedAt = dateTimeProvider.UtcNow,
            CreatedAt = dateTimeProvider.UtcNow
        };
    }

    public Result<Unit> ProgressStage(string stageName, bool success, string? output, IDateTimeProvider dateTimeProvider)
    {
        var stage = Stages.FirstOrDefault(s => s.Name == stageName);
        if (stage is null)
        {
            return Result<Unit>.Failure(Error.NotFound(
                "Pipeline.StageNotFound", $"Stage '{stageName}' not found in pipeline."));
        }

        if (stage.Status != StageStatus.Pending && stage.Status != StageStatus.InProgress)
        {
            return Result<Unit>.Failure(Error.Conflict(
                "Pipeline.StageAlreadyCompleted", $"Stage '{stageName}' is already {stage.Status}."));
        }

        stage.Status = success ? StageStatus.Passed : StageStatus.Failed;
        stage.Output = output;
        stage.CompletedAt = dateTimeProvider.UtcNow;

        if (!success && stage.IsRequired)
        {
            Status = PipelineStatus.Rejected;
            CompletedAt = dateTimeProvider.UtcNow;
            AddDomainEvent(new CertificationRejectedEvent(
                Id, TenantId, AgentId, stageName, output ?? "Stage failed", dateTimeProvider.UtcNow));
            return Result<Unit>.Success(Unit.Value);
        }

        // Advance to next pending stage
        var nextStage = Stages.FirstOrDefault(s => s.Status == StageStatus.Pending);
        if (nextStage is not null)
        {
            CurrentStage = nextStage.Name;
            nextStage.Status = StageStatus.InProgress;
            nextStage.StartedAt = dateTimeProvider.UtcNow;
        }
        else
        {
            // All stages complete
            if (Stages.Where(s => s.IsRequired).All(s => s.Status == StageStatus.Passed))
            {
                Status = PipelineStatus.Approved;
                CurrentStage = null;
                AddDomainEvent(new CertificationApprovedEvent(
                    Id, TenantId, AgentId, dateTimeProvider.UtcNow));
            }
            else
            {
                Status = PipelineStatus.AwaitingApproval;
            }
        }

        UpdatedAt = dateTimeProvider.UtcNow;
        return Result<Unit>.Success(Unit.Value);
    }

    public Result<Unit> Approve(Guid approvedBy, string? notes, IDateTimeProvider dateTimeProvider)
    {
        if (Status != PipelineStatus.AwaitingApproval && Status != PipelineStatus.Pending)
        {
            return Result<Unit>.Failure(Error.Conflict(
                "Pipeline.InvalidStatus", $"Cannot approve pipeline in status '{Status}'."));
        }

        Status = PipelineStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovalNotes = notes;
        CompletedAt = dateTimeProvider.UtcNow;
        CurrentStage = null;

        AddDomainEvent(new CertificationApprovedEvent(
            Id, TenantId, AgentId, dateTimeProvider.UtcNow));

        return Result<Unit>.Success(Unit.Value);
    }
}
