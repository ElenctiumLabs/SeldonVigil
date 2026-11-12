using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Security.Domain.Entities;

public class SecurityViolationTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public SecurityViolationTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void CreateWithValidParametersReturnsViolation()
    {
        // Arrange
        var policyId = SecurityPolicyId.New();
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var traceId = TraceId.New();
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "agent_id == 'test-agent'",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var violation = SecurityViolation.Create(
            policyId,
            tenantId,
            agentId,
            traceId,
            Severity.High,
            "Test violation description",
            ruleDefinition,
            _dateTimeProviderMock.Object);

        // Assert
        _ = violation.Id.Should().NotBeNull();
        _ = violation.PolicyId.Should().Be(policyId);
        _ = violation.TenantId.Should().Be(tenantId);
        _ = violation.AgentId.Should().Be(agentId);
        _ = violation.TraceId.Should().Be(traceId);
        _ = violation.Severity.Should().Be(Severity.High);
        _ = violation.Description.Should().Be("Test violation description");
        _ = violation.TriggeringRule.Should().Be(ruleDefinition);
        _ = violation.Status.Should().Be(ViolationStatus.Open);
        _ = violation.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public void CreateSetsStatusToOpen()
    {
        // Arrange
        var policyId = SecurityPolicyId.New();
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var traceId = TraceId.New();
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "test",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var violation = SecurityViolation.Create(
            policyId,
            tenantId,
            agentId,
            traceId,
            Severity.Medium,
            "Description",
            ruleDefinition,
            _dateTimeProviderMock.Object);

        // Assert
        _ = violation.Status.Should().Be(ViolationStatus.Open);
    }

    [Fact]
    public void CreateSetsCreatedAtToCurrentUtcTime()
    {
        // Arrange
        var expectedTime = DateTime.UtcNow;
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(expectedTime);
        var policyId = SecurityPolicyId.New();
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var traceId = TraceId.New();
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "test",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var violation = SecurityViolation.Create(
            policyId,
            tenantId,
            agentId,
            traceId,
            Severity.Low,
            "Description",
            ruleDefinition,
            _dateTimeProviderMock.Object);

        // Assert
        _ = violation.CreatedAt.Should().Be(expectedTime);
    }

    [Fact]
    public void CreateGeneratesNewId()
    {
        // Arrange
        var policyId = SecurityPolicyId.New();
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var traceId = TraceId.New();
        var ruleDefinition = new RuleDefinition
        {
            Type = RuleType.RegexMatch,
            Expression = "test",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var violation1 = SecurityViolation.Create(
            policyId,
            tenantId,
            agentId,
            traceId,
            Severity.High,
            "Description 1",
            ruleDefinition,
            _dateTimeProviderMock.Object);
        var violation2 = SecurityViolation.Create(
            policyId,
            tenantId,
            agentId,
            traceId,
            Severity.High,
            "Description 2",
            ruleDefinition,
            _dateTimeProviderMock.Object);

        // Assert
        _ = violation1.Id.Should().NotBe(violation2.Id);
    }
}
