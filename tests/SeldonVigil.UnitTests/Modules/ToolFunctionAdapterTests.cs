using System.Collections.ObjectModel;
using FluentAssertions;
using Microsoft.Extensions.AI;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Domain.Services;
using Xunit;

namespace SeldonVigil.UnitTests.Modules.Domain;

public class ToolFunctionAdapterTests
{
    private readonly StubToolOrchestrator _orchestrator = new();
    private readonly ToolFunctionAdapter _sut;

    public ToolFunctionAdapterTests()
    {
        _sut = new ToolFunctionAdapter(_orchestrator);
    }

    [Fact]
    public void GetToolFunctionsReturnsFunctionsForAllRegisteredTools()
    {
        // Arrange
        _orchestrator.Tools =
        [
            new AgentTool { Name = "query_traces", Description = "Query traces", Parameters = new() },
            new AgentTool { Name = "suspend_agent", Description = "Suspend agent", Parameters = new() },
        ];

        // Act
        var functions = _sut.GetToolFunctions();

        // Assert
        functions.Should().HaveCount(2);
        functions.Should().Contain(f => f.Name == "query_traces");
        functions.Should().Contain(f => f.Name == "suspend_agent");
    }

    [Fact]
    public void ConvertToFunctionCreatesFunctionWithCorrectNameAndDescription()
    {
        // Arrange
        var tool = new AgentTool
        {
            Name = "test_tool",
            Description = "A test tool for validation",
            Parameters = new()
        };

        // Act
        var func = _sut.ConvertToFunction(tool);

        // Assert
        func.Name.Should().Be("test_tool");
        func.Description.Should().Be("A test tool for validation");
    }

    [Fact]
    public void ConvertToFunctionGeneratesSchemaFromParameters()
    {
        // Arrange
        var tool = new AgentTool
        {
            Name = "query_tool",
            Description = "Query data",
            Parameters = new Dictionary<string, string>
            {
                ["agent_id"] = "string (required) - Agent ID to query",
                ["limit"] = "integer - Maximum results (default 100)"
            }
        };

        // Act
        var func = _sut.ConvertToFunction(tool);

        // Assert
        func.Name.Should().Be("query_tool");
        func.Should().NotBeNull();
    }

    [Fact]
    public async Task ConvertToFunctionCallsOrchestratorOnInvoke()
    {
        // Arrange
        var tool = new AgentTool
        {
            Name = "test_tool",
            Description = "Test",
            Parameters = new()
        };
        _orchestrator.LastResult = "{\"success\":true}";

        var func = _sut.ConvertToFunction(tool);

        // Act
        var result = await func.InvokeAsync(new AIFunctionArguments(), CancellationToken.None).AsTask();

        // Assert
        _orchestrator.LastToolName.Should().Be("test_tool");
    }

    [Fact]
    public void GetToolFunctionsEmptyToolsReturnsEmptyList()
    {
        // Arrange
        _orchestrator.Tools = [];

        // Act
        var functions = _sut.GetToolFunctions();

        // Assert
        functions.Should().BeEmpty();
    }
}

/// <summary>
/// Simple stub that avoids Moq proxy issues with internal interfaces.
/// </summary>
internal sealed class StubToolOrchestrator : IToolOrchestrator
{
    public IReadOnlyList<AgentTool> Tools { get; set; } = [];
    public string LastResult { get; set; } = "{}";
    public string? LastToolName { get; private set; }

    public ReadOnlyCollection<AgentTool> GetAvailableTools()
        => new(Tools.ToList());

    public Task<string> ExecuteAsync(string toolName, string argumentsJson, CancellationToken ct = default)
    {
        LastToolName = toolName;
        return Task.FromResult(LastResult);
    }
}
