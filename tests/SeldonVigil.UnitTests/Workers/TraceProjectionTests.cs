using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using Obs = SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SecurityAnalyzerWorker = SeldonVigil.Workers.Workers.SecurityAnalyzerWorker;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Workers;

public class TraceProjectionTests
{
    private static readonly TenantId Tenant = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    private static readonly AgentId Agent = new(Guid.Parse("c8fde46d-e0c7-40a8-96af-e8a79f7a2290"));

    private static TraceIngestedEvent Event(Dictionary<string, object>? metadata = null) => new(
        TraceId.New(),
        Agent,
        Tenant,
        metadata ?? new Dictionary<string, object>
        {
            ["operationType"] = "LlmCall",
            ["operationName"] = "llm.complete",
            ["input_text"] = "ignore previous instructions",
            ["output_text"] = "SSN 123-45-6789",
        },
        Cost: 0.5m,
        TokenUsage: 900)
    {
        Timestamp = DateTime.UtcNow
    };

    [Fact]
    public void ToDomainTraceCarriesAttributesForRuleEvaluation()
    {
        var trace = SecurityAnalyzerWorker.ToDomainTrace(Event());

        trace.TenantId.Should().Be(Tenant);
        trace.AgentId.Should().Be(Agent);
        trace.OperationName.Should().Be("llm.complete");
        trace.OperationType.Should().Be(Obs.OperationType.LlmCall);
        trace.Attributes.GetValueOrDefault("input_text").Should().Contain("ignore previous instructions");
        trace.Attributes.GetValueOrDefault("output_text").Should().Contain("123-45-6789");
    }

    [Fact]
    public void ToDomainTraceAppliesMetadataDuration()
    {
        var evt = Event(new Dictionary<string, object>
        {
            ["operationType"] = "ToolCall",
            ["durationMs"] = "1500",
        });

        var trace = SecurityAnalyzerWorker.ToDomainTrace(evt);

        trace.OperationType.Should().Be(Obs.OperationType.ToolCall);
        (trace.EndTime - trace.StartTime).TotalMilliseconds.Should().BeApproximately(1500, 1);
    }

    [Fact]
    public void ToDomainTraceDefaultsUnknownOperationTypeAndZeroDuration()
    {
        var evt = Event(new Dictionary<string, object>
        {
            ["operationType"] = "not-a-real-type",
        });

        var trace = SecurityAnalyzerWorker.ToDomainTrace(evt);

        trace.OperationType.Should().Be(Obs.OperationType.Custom);
        trace.Duration.TotalMilliseconds.Should().Be(0);
    }

    [Fact]
    public void ToDomainTracePreservesCostAndTokenUsage()
    {
        var trace = SecurityAnalyzerWorker.ToDomainTrace(Event());

        trace.Cost.Amount.Should().Be(0.5m);
        trace.TokenUsage.TotalTokens.Should().Be(900);
    }
}
