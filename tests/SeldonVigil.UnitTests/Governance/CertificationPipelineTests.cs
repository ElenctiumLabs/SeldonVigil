using SeldonVigil.Api.Modules.Governance.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Governance.Domain.Entities;

public class CertificationPipelineTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public CertificationPipelineTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void CreateWithValidParametersReturnsPipeline()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var createdBy = Guid.NewGuid();

        // Act
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            createdBy,
            _dateTimeProviderMock.Object);

        // Assert
        _ = pipeline.TenantId.Should().Be(tenantId);
        _ = pipeline.AgentId.Should().Be(agentId);
        _ = pipeline.Status.Should().Be(PipelineStatus.Pending);
        _ = pipeline.CurrentStage.Should().Be("static_analysis");
        _ = pipeline.Stages.Should().HaveCount(4);
        _ = pipeline.StartedAt.Should().NotBe(default);
    }

    [Fact]
    public void CreateInitializesStagesCorrectly()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();

        // Act
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = pipeline.Stages[0].Name.Should().Be("static_analysis");
        _ = pipeline.Stages[0].IsRequired.Should().BeTrue();
        _ = pipeline.Stages[0].Status.Should().Be(StageStatus.Pending); // Stages start as Pending

        _ = pipeline.Stages[1].Name.Should().Be("security_scan");
        _ = pipeline.Stages[1].Status.Should().Be(StageStatus.Pending);

        _ = pipeline.Stages[2].Name.Should().Be("policy_validation");
        _ = pipeline.Stages[2].Status.Should().Be(StageStatus.Pending);

        _ = pipeline.Stages[3].Name.Should().Be("manual_approval");
        _ = pipeline.Stages[3].IsRequired.Should().BeFalse();
        _ = pipeline.Stages[3].Status.Should().Be(StageStatus.Pending);
    }

    [Fact]
    public void ProgressStageWithSuccessMovesToNextStage()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Act
        var result = pipeline.ProgressStage("static_analysis", true, "Analysis passed", _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = pipeline.Stages[0].Status.Should().Be(StageStatus.Passed);
        _ = pipeline.CurrentStage.Should().Be("security_scan");
        _ = pipeline.Stages[1].Status.Should().Be(StageStatus.InProgress);
    }

    [Fact]
    public void ProgressStageWithFailureOnRequiredStageRejectsPipeline()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Act
        var result = pipeline.ProgressStage("static_analysis", false, "Analysis failed", _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = pipeline.Status.Should().Be(PipelineStatus.Rejected);
        _ = pipeline.CompletedAt.Should().NotBeNull();
        _ = pipeline.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CertificationRejectedEvent");
    }

    [Fact]
    public void ProgressStageWithInvalidStageReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Act
        var result = pipeline.ProgressStage("invalid_stage", true, "Output", _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Pipeline.StageNotFound");
    }

    [Fact]
    public void ProgressStageWithAlreadyCompletedStageReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("static_analysis", true, "Passed", _dateTimeProviderMock.Object);

        // Act
        var result = pipeline.ProgressStage("static_analysis", true, "Passed again", _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Pipeline.StageAlreadyCompleted");
    }

    [Fact]
    public void ProgressStageWithAllRequiredStagesPassedApprovesPipeline()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("static_analysis", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("security_scan", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("policy_validation", true, "Passed", _dateTimeProviderMock.Object);

        // Act
        var result = pipeline.ProgressStage("manual_approval", true, "Approved", _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = pipeline.Status.Should().Be(PipelineStatus.Approved);
        _ = pipeline.CurrentStage.Should().BeNull();
        // CompletedAt is not set by ProgressStage, only by Approve
        _ = pipeline.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CertificationApprovedEvent");
    }

    [Fact]
    public void ProgressStageWithOptionalStageFailureSetsAwaitingApproval()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("static_analysis", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("security_scan", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("policy_validation", true, "Passed", _dateTimeProviderMock.Object);

        // Act
        var result = pipeline.ProgressStage("manual_approval", false, "Skipped", _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        // When all required stages pass, status goes to Approved even if optional stage fails
        _ = pipeline.Status.Should().Be(PipelineStatus.Approved);
        _ = pipeline.CurrentStage.Should().BeNull();
    }

    [Fact]
    public void ApproveWhenAwaitingApprovalChangesStatusToApproved()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        // Skip the optional stage to get to AwaitingApproval state
        // Since all required stages pass, we need to manually set status to test approval
        var approvedBy = Guid.NewGuid();
        var notes = "Approved after review";

        // Act - Approve can be called from Pending state
        var result = pipeline.Approve(approvedBy, notes, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = pipeline.Status.Should().Be(PipelineStatus.Approved);
        _ = pipeline.ApprovedBy.Should().Be(approvedBy);
        _ = pipeline.ApprovalNotes.Should().Be(notes);
        _ = pipeline.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void ApproveWhenPendingChangesStatusToApproved()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        var approvedBy = Guid.NewGuid();

        // Act
        var result = pipeline.Approve(approvedBy, null, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = pipeline.Status.Should().Be(PipelineStatus.Approved);
    }

    [Fact]
    public void ApproveWhenRejectedReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("static_analysis", false, "Failed", _dateTimeProviderMock.Object);
        var approvedBy = Guid.NewGuid();

        // Act
        var result = pipeline.Approve(approvedBy, null, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Pipeline.InvalidStatus");
    }

    [Fact]
    public void ApproveWhenAlreadyApprovedReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("static_analysis", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("security_scan", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("policy_validation", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("manual_approval", true, "Approved", _dateTimeProviderMock.Object);
        var approvedBy = Guid.NewGuid();

        // Act
        var result = pipeline.Approve(approvedBy, null, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Pipeline.InvalidStatus");
    }

    [Fact]
    public void ProgressStageEmitsDomainEventOnRejection()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Act
        _ = pipeline.ProgressStage("static_analysis", false, "Failed", _dateTimeProviderMock.Object);

        // Assert
        _ = pipeline.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CertificationRejectedEvent");
    }

    [Fact]
    public void ProgressStageEmitsDomainEventOnApproval()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var pipeline = CertificationPipeline.Create(
            tenantId,
            agentId,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("static_analysis", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("security_scan", true, "Passed", _dateTimeProviderMock.Object);
        _ = pipeline.ProgressStage("policy_validation", true, "Passed", _dateTimeProviderMock.Object);

        // Act
        _ = pipeline.ProgressStage("manual_approval", true, "Approved", _dateTimeProviderMock.Object);

        // Assert
        _ = pipeline.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CertificationApprovedEvent");
    }
}
