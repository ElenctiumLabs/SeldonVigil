using SeldonVigil.Sdk.Observability;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.SdkTests;

public class ObservabilityClientTests
{
    [Fact]
    public async Task QueryTracesAsyncParsesBareArrayResponse()
    {
        var (client, handler) = StubApiHandler.CreateObservabilityClient();
        handler.RouteJson("/api/v1/observability/traces", """
            [
              { "id": "44444444-4444-4444-4444-444444444444", "agentId": "c8fde46d-e0c7-40a8-96af-e8a79f7a2290",
                "status": "Ok", "createdAt": "2026-08-23T10:00:00Z", "durationMs": 123, "error": null, "costUsd": 0.01 },
              { "id": "55555555-5555-5555-5555-555555555555", "agentId": "c8fde46d-e0c7-40a8-96af-e8a79f7a2290",
                "status": "Degraded", "createdAt": "2026-08-23T10:05:00Z", "durationMs": 900, "error": "timeout", "costUsd": null }
            ]
            """);

        var traces = await client.QueryTracesAsync(new TraceQuery { Limit = 10 });

        traces.Should().HaveCount(2);
        traces[0].Id.Should().Be("44444444-4444-4444-4444-444444444444");
        traces[0].Status.Should().Be(TraceStatus.Ok);
        traces[0].DurationMs.Should().Be(123);
        traces[0].Cost.Should().Be(0.01m);
        traces[1].Status.Should().Be(TraceStatus.Degraded);
    }

    [Fact]
    public async Task QueryTracesAsyncParsesWidenedContractProperties()
    {
        var (client, handler) = StubApiHandler.CreateObservabilityClient();
        handler.RouteJson("/api/v1/observability/traces", """
            [
              {
                "id": "11111111-1111-1111-1111-111111111111",
                "agentId": "22222222-2222-2222-2222-222222222222",
                "agentName": "SeldonVigil Core",
                "parentTraceId": "00000000-0000-0000-0000-000000000000",
                "operationName": "agent.thesis.generate",
                "operationType": "llm",
                "modelName": "gpt-4o",
                "status": "Ok",
                "createdAt": "2026-09-11T12:00:00Z",
                "durationMs": 450,
                "costUsd": 0.025,
                "totalTokens": 1200,
                "attributes": { "phase": "debate", "speaker": "Thesis" },
                "events": [{ "name": "Started", "value": "prompt_sent" }],
                "children": []
              }
            ]
            """);

        var traces = await client.QueryTracesAsync(new TraceQuery());

        traces.Should().HaveCount(1);
        var trace = traces[0];
        trace.Id.Should().Be("11111111-1111-1111-1111-111111111111");
        trace.AgentId.Should().Be("22222222-2222-2222-2222-222222222222");
        trace.AgentName.Should().Be("SeldonVigil Core");
        trace.ParentTraceId.Should().Be("00000000-0000-0000-0000-000000000000");
        trace.OperationName.Should().Be("agent.thesis.generate");
        trace.OperationType.Should().Be("llm");
        trace.ModelName.Should().Be("gpt-4o");
        trace.Status.Should().Be(TraceStatus.Ok);
        trace.DurationMs.Should().Be(450);
        trace.Cost.Should().Be(0.025m);
        trace.TokenUsage.Should().NotBeNull();
        trace.TokenUsage!.InputTokens.Should().Be(1200);
        trace.Attributes.Should().ContainKey("phase").WhoseValue.Should().Be("debate");
        trace.Events.Should().HaveCount(1);
        trace.Events[0].Name.Should().Be("Started");
    }
}

