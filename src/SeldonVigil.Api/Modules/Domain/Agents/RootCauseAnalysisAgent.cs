using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Agents;

/// <summary>
/// Specialized agent for automated root cause analysis of anomalies.
/// Uses chain-of-thought reasoning with tool access to telemetry data.
/// </summary>
internal sealed class RootCauseAnalysisAgent(
    IChatClient chatClient,
    IToolOrchestrator toolOrchestrator,
    ToolFunctionAdapter toolFunctionAdapter,
    IMemoryService memoryService,
    IPromptManager promptManager,
    IEvaluationService evaluationService,
    ICostTracker costTracker,
    ILogger<RootCauseAnalysisAgent> logger)
    : BaseReasoningAgent<RCARequest, RCAResponse>(chatClient, toolOrchestrator, toolFunctionAdapter, memoryService, promptManager,
        evaluationService, costTracker, logger)
{
    protected override string GetPromptName() => "root_cause_analysis";

    protected override Dictionary<string, object> BuildPromptVariables(RCARequest request, MemoryContext memory)
    {
        return new Dictionary<string, object>
        {
            ["anomaly_type"] = request.AnomalyType,
            ["agent_id"] = request.AgentId.ToString(),
            ["trace_id"] = request.TraceId?.ToString() ?? "N/A",
            ["time_range"] = $"{request.AnomalyStartTime:O} to {request.AnomalyEndTime:O}",
            ["historical_patterns"] = memory.Patterns,
            ["similar_incidents"] = memory.RelevantMemories.Select(m => new
            {
                m.Content,
                m.Score,
                m.Timestamp
            }).ToList()
        };
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    protected override RCAResponse ParseResponse(string responseText)
    {
        // Extract JSON from markdown code blocks if present
        var jsonMatch = Regex.Match(responseText, @"```(?:json)?\s*([\s\S]*?)\s*```");
        var json = jsonMatch.Success ? jsonMatch.Groups[1].Value : responseText;

        try
        {
            return JsonSerializer.Deserialize<RCAResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to deserialize RCA response");
        }
        catch (JsonException)
        {
            // Fallback: wrap raw text in structured format
            return new RCAResponse
            {
                ProbableRootCause = responseText[..Math.Min(500, responseText.Length)],
                Confidence = 0.3,
                ConfidenceLabel = "Low",
                Evidence = new List<string> { "Raw text response - parsing failed" },
                ContributingFactors = new List<string>(),
                RecommendedActions = new List<RCAAction>
                {
                    new("Review raw analysis output", "P1", "SRE Team")
                },
                RelatedTraceIds = new List<string>()
            };
        }
    }

    protected override async Task<MemoryContext> RetrieveMemoryAsync(
        RCARequest request, ReasoningContext context, CancellationToken ct)
    {
        var memories = await _memoryService.SearchAgentMemoryAsync(
            request.AgentId,
            context.TenantId,
            $"anomaly {request.AnomalyType} {request.AnomalyStartTime:yyyy-MM}",
            10,
            ct).ConfigureAwait(false);

        var patterns = await _memoryService.GetBehavioralPatternsAsync(
            request.AgentId,
            context.TenantId,
            5,
            ct).ConfigureAwait(false);

        return new MemoryContext(memories, patterns, new Dictionary<string, object>());
    }

    protected override async Task StoreMemoryAsync(
        RCARequest request,
        RCAResponse? response,
        List<ReasoningStep> steps,
        List<ToolCallRecord> toolCalls,
        ReasoningContext context,
        CancellationToken ct)
    {
        if (response is null)
        {
            return;
        }

        var memoryContent = $@"
            RCA for {request.AnomalyType} on agent {request.AgentId}
            Root cause: {response.ProbableRootCause}
            Confidence: {response.ConfidenceLabel} ({response.Confidence:P0})
            Actions taken: {string.Join(", ", response.RecommendedActions.Select(a => a.Action))}
        ";

        await _memoryService.StoreAgentMemoryAsync(
            request.AgentId,
            context.TenantId,
            memoryContent,
            MemoryType.Trace,
            new Dictionary<string, string>
            {
                ["anomaly_type"] = request.AnomalyType,
                ["confidence"] = response.Confidence.ToString(),
                ["trace_id"] = request.TraceId?.ToString() ?? "N/A"
            },
            ct).ConfigureAwait(false);
    }
}
