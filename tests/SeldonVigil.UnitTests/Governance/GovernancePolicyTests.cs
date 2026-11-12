using SeldonVigil.Api.Modules.Governance.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Governance.Domain.Entities;

public class GovernancePolicyTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public GovernancePolicyTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void CreateWithValidParametersReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";

        // Act
        var result = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Name.Should().Be("Test Policy");
        _ = result.Value.Status.Should().Be(PolicyStatus.Draft);
        _ = result.Value.Version.Should().Be(1);
        _ = result.Value.DefinitionYaml.Should().Be(definitionYaml);
    }

    [Fact]
    public void CreateWithInvalidNameReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";

        // Act
        var result = GovernancePolicy.Create(
            tenantId,
            "", // Invalid: empty
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.InvalidName");
    }

    [Fact]
    public void CreateWithNameTooLongReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";
        var longName = new string('a', 256);

        // Act
        var result = GovernancePolicy.Create(
            tenantId,
            longName,
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.InvalidName");
    }

    [Fact]
    public void CreateWithEmptyDefinitionYamlReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();

        // Act
        var result = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            "", // Invalid: empty
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.InvalidDefinition");
    }

    [Fact]
    public void CreateWithTooShortDefinitionYamlReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var shortDefinition = "abc"; // Less than 5 characters

        // Act
        var result = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            shortDefinition,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.InvalidDefinition");
    }

    [Fact]
    public void ActivateWhenDraftChangesStatusToActive()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";
        var policy = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var activatedBy = Guid.NewGuid();

        // Act
        var result = policy.Activate(activatedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = policy.Status.Should().Be(PolicyStatus.Active);
        _ = policy.ActivatedAt.Should().NotBeNull();
        _ = policy.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "GovernancePolicyActivatedEvent");
    }

    [Fact]
    public void ActivateWhenNotDraftReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";
        var policy = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = policy.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var activatedBy = Guid.NewGuid();

        // Act
        var result = policy.Activate(activatedBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.InvalidStatus");
    }

    [Fact]
    public void CreateNewVersionWhenActiveCreatesNewVersion()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";
        var policy = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = policy.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var newDefinitionYaml = "version: 2\nrules:\n  - name: test2";
        var createdBy = Guid.NewGuid();

        // Act
        var result = policy.CreateNewVersion(newDefinitionYaml, createdBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Version.Should().Be(2);
        _ = result.Value.PreviousVersionId.Should().Be(policy.Id.ToString());
        _ = policy.Status.Should().Be(PolicyStatus.Deprecated);
        _ = policy.DeprecatedAt.Should().NotBeNull();
    }

    [Fact]
    public void CreateNewVersionWhenNotActiveReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";
        var policy = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var newDefinitionYaml = "version: 2\nrules:\n  - name: test2";
        var createdBy = Guid.NewGuid();

        // Act
        var result = policy.CreateNewVersion(newDefinitionYaml, createdBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.NotActive");
    }

    [Fact]
    public void CreateNewVersionWithInvalidDefinitionReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";
        var policy = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = policy.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var invalidDefinition = ""; // Invalid: empty
        var createdBy = Guid.NewGuid();

        // Act
        var result = policy.CreateNewVersion(invalidDefinition, createdBy, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.InvalidDefinition");
    }

    [Fact]
    public void CreateNewVersionEmitsDomainEvent()
    {
        // Arrange
        var tenantId = TenantId.New();
        var definitionYaml = "version: 1\nrules:\n  - name: test";
        var policy = GovernancePolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataAccess,
            definitionYaml,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = policy.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        var newDefinitionYaml = "version: 2\nrules:\n  - name: test2";
        var createdBy = Guid.NewGuid();

        // Act
        _ = policy.CreateNewVersion(newDefinitionYaml, createdBy, _dateTimeProviderMock.Object);

        // Assert
        _ = policy.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "GovernancePolicyVersionedEvent");
    }
}
