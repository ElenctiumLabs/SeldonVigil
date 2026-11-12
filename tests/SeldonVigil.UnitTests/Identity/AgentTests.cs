using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Identity.Domain.Entities;

public class AgentTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public AgentTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void CreateWithValidParametersReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read", "write" }
        };

        // Act
        var result = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Status.Should().Be(AgentStatus.Draft);
        _ = result.Value.CapabilityManifestHash.Should().NotBeNullOrEmpty();
        _ = result.Value.ExternalId.Should().Be("external-123");
        _ = result.Value.Name.Should().Be("Test Agent");
        _ = result.Value.ModelName.Should().Be("gpt-4");
    }

    [Fact]
    public void CreateWithInvalidExternalIdReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };

        // Act
        var result = Agent.Create(
            tenantId,
            "", // Invalid: empty
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.InvalidExternalId");
    }

    [Fact]
    public void CreateWithExternalIdTooLongReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var longExternalId = new string('a', 256);

        // Act
        var result = Agent.Create(
            tenantId,
            longExternalId,
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.InvalidExternalId");
    }

    [Fact]
    public void CreateWithInvalidNameReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };

        // Act
        var result = Agent.Create(
            tenantId,
            "external-123",
            "", // Invalid: empty
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.InvalidName");
    }

    [Fact]
    public void ActivateWhenInDraftStatusReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var activatedBy = Guid.NewGuid();

        // Act
        var result = agent.Activate(activatedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = agent.Status.Should().Be(AgentStatus.Active);
        _ = agent.ActivatedAt.Should().NotBeNull();
        _ = agent.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "AgentActivatedEvent");
    }

    [Fact]
    public void ActivateWhenInPendingCertificationStatusReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        // Manually set to PendingCertification (in real code this would be through a domain method)
        var statusProperty = typeof(Agent).GetProperty("Status");
        statusProperty?.SetValue(agent, AgentStatus.PendingCertification);
        var activatedBy = Guid.NewGuid();

        // Act
        var result = agent.Activate(activatedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = agent.Status.Should().Be(AgentStatus.Active);
    }

    [Fact]
    public void ActivateWhenNotInDraftOrPendingCertificationReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        // Manually set to Active
        var statusProperty = typeof(Agent).GetProperty("Status");
        statusProperty?.SetValue(agent, AgentStatus.Active);
        var activatedBy = Guid.NewGuid();

        // Act
        var result = agent.Activate(activatedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.InvalidStatus");
    }

    [Fact]
    public void SuspendWhenActiveWithReasonReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var suspendedBy = Guid.NewGuid();
        var reason = "Suspicious activity detected";

        // Act
        var result = agent.Suspend(reason, suspendedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = agent.Status.Should().Be(AgentStatus.Suspended);
        _ = agent.SuspendedAt.Should().NotBeNull();
        _ = agent.SuspendedReason.Should().Be(reason);
        _ = agent.IdentityTokenJti.Should().BeNull();
        _ = agent.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "AgentSuspendedEvent");
    }

    [Fact]
    public void SuspendWhenNotActiveReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var suspendedBy = Guid.NewGuid();
        var reason = "Suspicious activity detected";

        // Act
        var result = agent.Suspend(reason, suspendedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.InvalidStatus");
    }

    [Fact]
    public void SuspendWithoutReasonReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var suspendedBy = Guid.NewGuid();

        // Act
        var result = agent.Suspend("", suspendedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.SuspendReasonRequired");
    }

    [Fact]
    public void RevokeWhenActiveWithReasonReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var revokedBy = Guid.NewGuid();
        var reason = "Security violation";

        // Act
        var result = agent.Revoke(reason, revokedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = agent.Status.Should().Be(AgentStatus.Revoked);
        _ = agent.RevokedAt.Should().NotBeNull();
        _ = agent.RevokedReason.Should().Be(reason);
        _ = agent.IdentityTokenJti.Should().BeNull();
        _ = agent.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "AgentRevokedEvent");
    }

    [Fact]
    public void RevokeWhenAlreadyRevokedReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = agent.Revoke("Reason 1", Guid.NewGuid(), _dateTimeProviderMock.Object);
        var revokedBy = Guid.NewGuid();
        var reason = "Another reason";

        // Act
        var result = agent.Revoke(reason, revokedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.AlreadyRevoked");
    }

    [Fact]
    public void RevokeWithoutReasonReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var revokedBy = Guid.NewGuid();

        // Act
        var result = agent.Revoke("", revokedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.RevokeReasonRequired");
    }

    [Fact]
    public void UpdateManifestWithDifferentManifestReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var newManifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read", "write" }
        };

        // Act
        var result = agent.UpdateManifest(newManifest, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = agent.CapabilityManifestHash.Should().NotBe(manifest.ComputeHash());
    }

    [Fact]
    public void UpdateManifestWhenActiveRevertsToDraft()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var newManifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read", "write" }
        };

        // Act
        var result = agent.UpdateManifest(newManifest, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = agent.Status.Should().Be(AgentStatus.Draft);
    }

    [Fact]
    public void IssueIdentityTokenWhenActiveReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var jti = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var result = agent.IssueIdentityToken(jti, expiresAt, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = agent.IdentityTokenJti.Should().Be(jti);
        _ = agent.IdentityTokenIssuedAt.Should().NotBeNull();
        _ = agent.IdentityTokenExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void IssueIdentityTokenWhenNotActiveReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" }
        };
        var agent = Agent.Create(
            tenantId,
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var jti = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var result = agent.IssueIdentityToken(jti, expiresAt, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Agent.NotActive");
    }
}
