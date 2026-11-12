using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Observability.Domain.Entities;

public class TraceTests
{
    [Fact]
    public void CreateWithValidParametersReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var parentTraceId = TraceId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var attributes = new Dictionary<string, string> { { "key", "value" } };
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");

        // Act
        var result = Trace.Create(
            tenantId,
            agentId,
            parentTraceId,
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            attributes,
            new List<TraceEvent>(),
            new List<TraceLink>(),
            tokenUsage,
            cost,
            false);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.OperationName.Should().Be("test-operation");
        _ = result.Value.TenantId.Should().Be(tenantId);
        _ = result.Value.AgentId.Should().Be(agentId);
        _ = result.Value.ParentTraceId.Should().Be(parentTraceId);
        _ = result.Value.Duration.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateWithInvalidOperationNameReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");

        // Act
        var result = Trace.Create(
            tenantId,
            agentId,
            null,
            "", // Invalid: empty
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            new List<TraceEvent>(),
            new List<TraceLink>(),
            tokenUsage,
            cost);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Trace.InvalidOperationName");
    }

    [Fact]
    public void CreateWithEndTimeBeforeStartTimeReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(-1); // Invalid: before start time
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");

        // Act
        var result = Trace.Create(
            tenantId,
            agentId,
            null,
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            new List<TraceEvent>(),
            new List<TraceLink>(),
            tokenUsage,
            cost);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Trace.InvalidTiming");
    }

    [Fact]
    public void DurationCalculatesCorrectly()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMilliseconds(500);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");

        // Act
        var result = Trace.Create(
            tenantId,
            agentId,
            null,
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            new List<TraceEvent>(),
            new List<TraceLink>(),
            tokenUsage,
            cost);

        // Assert
        _ = result.Value!.Duration.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void IsRootSpanWhenParentTraceIdIsNullReturnsTrue()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");

        // Act
        var result = Trace.Create(
            tenantId,
            agentId,
            null, // No parent
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            new List<TraceEvent>(),
            new List<TraceLink>(),
            tokenUsage,
            cost);

        // Assert
        _ = result.Value!.IsRootSpan.Should().BeTrue();
    }

    [Fact]
    public void IsRootSpanWhenParentTraceIdIsSetReturnsFalse()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var parentTraceId = TraceId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");

        // Act
        var result = Trace.Create(
            tenantId,
            agentId,
            parentTraceId,
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            new List<TraceEvent>(),
            new List<TraceLink>(),
            tokenUsage,
            cost);

        // Assert
        _ = result.Value!.IsRootSpan.Should().BeFalse();
    }

    [Fact]
    public void AddEventAddsEventToEventsList()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");
        var trace = Trace.Create(
            tenantId,
            agentId,
            null,
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            new List<TraceEvent>(),
            new List<TraceLink>(),
            tokenUsage,
            cost).Value!;
        var newEvent = new TraceEvent("test-event", DateTime.UtcNow, new Dictionary<string, string>());

        // Act
        trace.AddEvent(newEvent);

        // Assert
        _ = trace.Events.Should().ContainSingle(e => e.Name == "test-event");
    }

    [Fact]
    public void HasSilentFailureWithDeclaredToolsButNoExecutionReturnsTrue()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");
        var events = new List<TraceEvent>
        {
            new TraceEvent("tool.declared", DateTime.UtcNow, new Dictionary<string, string> { { "tool_name", "search" } })
        };
        var trace = Trace.Create(
            tenantId,
            agentId,
            null,
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            events,
            new List<TraceLink>(),
            tokenUsage,
            cost).Value!;

        // Act
        var result = trace.HasSilentFailure();

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public void HasSilentFailureWithExecutedToolsReturnsFalse()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");
        var events = new List<TraceEvent>
        {
            new TraceEvent("tool.declared", DateTime.UtcNow, new Dictionary<string, string> { { "tool_name", "search" } }),
            new TraceEvent("tool.executed", DateTime.UtcNow, new Dictionary<string, string> { { "tool_name", "search" } })
        };
        var trace = Trace.Create(
            tenantId,
            agentId,
            null,
            "test-operation",
            OperationType.ToolCall,
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            events,
            new List<TraceLink>(),
            tokenUsage,
            cost).Value!;

        // Act
        var result = trace.HasSilentFailure();

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void HasSilentFailureWithNonToolOperationReturnsFalse()
    {
        // Arrange
        var tenantId = TenantId.New();
        var agentId = AgentId.New();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(1);
        var tokenUsage = new TokenUsage(100, 50, 150);
        var cost = new Cost(0.01m, "USD");
        var events = new List<TraceEvent>
        {
            new TraceEvent("tool.declared", DateTime.UtcNow, new Dictionary<string, string> { { "tool_name", "search" } })
        };
        var trace = Trace.Create(
            tenantId,
            agentId,
            null,
            "test-operation",
OperationType.LlmCall, // Not a tool call
            startTime,
            endTime,
TraceStatus.Ok,
            new Dictionary<string, string>(),
            events,
            new List<TraceLink>(),
            tokenUsage,
            cost).Value!;

        // Act
        var result = trace.HasSilentFailure();

        // Assert
        _ = result.Should().BeFalse();
    }
}
