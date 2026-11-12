using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Security.Domain.Entities;

public class SecurityPolicyTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public SecurityPolicyTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void CreateWithValidParametersReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "agent_id == 'test-agent'",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = SecurityPolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataLossPrevention,
            ruleDefinition,
            Severity.High,
            null,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Name.Should().Be("Test Policy");
        _ = result.Value.Status.Should().Be(PolicyStatus.Active);
        _ = result.Value.Severity.Should().Be(Severity.High);
    }

    [Fact]
    public void CreateWithInvalidNameReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "agent_id == 'test-agent'",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = SecurityPolicy.Create(
            tenantId,
            "", // Invalid: empty
            "Test Description",
            PolicyCategory.DataLossPrevention,
            ruleDefinition,
            Severity.High,
            null,
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
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "agent_id == 'test-agent'",
            Parameters = new Dictionary<string, object>()
        };
        var longName = new string('a', 101);

        // Act
        var result = SecurityPolicy.Create(
            tenantId,
            longName,
            "Test Description",
            PolicyCategory.DataLossPrevention,
            ruleDefinition,
            Severity.High,
            null,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Policy.InvalidName");
    }

    [Fact]
    public void DisableWhenActiveChangesStatusToDisabled()
    {
        // Arrange
        var tenantId = TenantId.New();
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "agent_id == 'test-agent'",
            Parameters = new Dictionary<string, object>()
        };
        var policy = SecurityPolicy.Create(
            tenantId,
            "Test Policy",
            "Test Description",
            PolicyCategory.DataLossPrevention,
            ruleDefinition,
            Severity.High,
            null,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var disabledBy = Guid.NewGuid();

        // Act
        policy.Disable(disabledBy, _dateTimeProviderMock.Object);

        // Assert
        _ = policy.Status.Should().Be(PolicyStatus.Disabled);
        _ = policy.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PolicyDisabledEvent");
    }
}
