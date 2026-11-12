using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.Domain.Services;
using SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.AIReasoning.Application.Commands;

internal sealed partial class GenerateRootCauseAnalysisCommandHandler(
    IChatClient chatClient,
    ITraceStore traceStore,
    ISecurityViolationRepository violationRepository,
    ICostBudgetRepository budgetRepository,
    LongTermMemoryService memoryService,
    ILogger<GenerateRootCauseAnalysisCommandHandler> logger)
    : ICommandHandler<GenerateRootCauseAnalysisCommand, RootCauseAnalysisResult>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "RCA generated for {AnomalyType} on agent {AgentId}: {Confidence:P0} confidence")]
    private static partial void LogRcaGenerated(ILogger logger, string anomalyType, string agentId, double confidence);

    [RequiresDynamicCode("Calls SeldonVigil.Api.Modules.AIReasoning.Application.Commands.GenerateRootCauseAnalysisCommandHandler.ParseRCAResponse(String)")]
    [RequiresUnreferencedCode("Calls SeldonVigil.Api.Modules.AIReasoning.Application.Commands.GenerateRootCauseAnalysisCommandHandler.ParseRCAResponse(String)")]
    public async Task<Result<RootCauseAnalysisResult>> Handle(
        GenerateRootCauseAnalysisCommand request, CancellationToken ct)
    {
        // Gather evidence
        var traces = await traceStore.GetAgentTracesAsync(
            request.AgentId,
            request.AnomalyStartTime.AddHours(-1),
            request.AnomalyEndTime.AddHours(1),
            1000,
            ct).ConfigureAwait(false);

        var violations = await violationRepository.GetByAgentAndDateRangeAsync(
            request.AgentId,
            request.AnomalyStartTime.AddDays(-7),
            request.AnomalyEndTime,
            ct).ConfigureAwait(false);

        var budgets = await budgetRepository.GetApplicableBudgetsAsync(
            request.TenantId, request.AgentId, ct).ConfigureAwait(false);

        var memory = await memoryService.SearchAgentMemoryAsync(
            request.AgentId,
            request.TenantId,
            $"anomalies {request.AnomalyType}",
            5,
            ct).ConfigureAwait(false);

        // Build structured prompt for reasoning model
        var prompt = BuildRCAPrompt(request, traces, violations, budgets, memory);

        // Use reasoning model (o3 or Claude 3.7 Sonnet)
        var response = await chatClient.GetResponseAsync(
            new ChatMessage(ChatRole.User, prompt),
            new ChatOptions
            {
                ModelId = "o3-mini", // or claude-3-7-sonnet
                Temperature = 0.1f, // Low temperature for deterministic reasoning
                MaxOutputTokens = 4000
            },
            ct).ConfigureAwait(false);

        var rca = ParseRCAResponse(response.Text);

        LogRcaGenerated(logger, request.AnomalyType, request.AgentId.Value.ToString(), rca.Confidence);

        return Result.Success(rca);
    }

    private static string BuildRCAPrompt(
        GenerateRootCauseAnalysisCommand request,
        IReadOnlyList<TraceSummaryDto> traces,
        IReadOnlyList<SecurityViolation> violations,
        IReadOnlyList<CostBudget> budgets,
        IReadOnlyList<MemoryResult> memory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a senior Site Reliability Engineer analyzing an AI agent anomaly.");
        sb.AppendLine();
        sb.AppendLine($"ANOMALY TYPE: {request.AnomalyType}");
        sb.AppendLine($"AGENT ID: {request.AgentId}");
        sb.AppendLine($"TIME RANGE: {request.AnomalyStartTime:O} to {request.AnomalyEndTime:O}");
        sb.AppendLine();

        sb.AppendLine("TRACE DATA (last 1000 traces):");
        foreach (var trace in traces.Take(20))
        {
            sb.AppendLine($"- {trace.OperationName} | {trace.StatusCode} | {trace.DurationMs:F0}ms | ${trace.CostUsd:F4} | {trace.TotalTokens} tokens");
        }
        sb.AppendLine($"Total traces in window: {traces.Count}");
        sb.AppendLine($"Error rate: {(traces.Count > 0 ? traces.Count(t => t.StatusCode == "error") / (double)traces.Count * 100 : 0):F1}%");
        sb.AppendLine($"Avg latency: {(traces.Count > 0 ? traces.Average(t => t.DurationMs) : 0):F0}ms");
        sb.AppendLine($"Total cost: ${traces.Sum(t => t.CostUsd):F2}");
        sb.AppendLine();

        sb.AppendLine("RECENT SECURITY VIOLATIONS:");
        foreach (var v in violations.Take(10))
        {
            sb.AppendLine($"- {v.Severity}: {v.Description} ({v.CreatedAt:O})");
        }
        sb.AppendLine();

        sb.AppendLine("BUDGET STATUS:");
        foreach (var b in budgets)
        {
            sb.AppendLine($"- {b.Name}: ${b.CurrentSpend.Amount:F2} / ${b.BudgetAmount.Amount:F2} ({b.CurrentSpend.UtilizationPercentage(b.BudgetAmount):F1}%)");
        }
        sb.AppendLine();

        sb.AppendLine("HISTORICAL CONTEXT:");
        foreach (var m in memory)
        {
            sb.AppendLine($"- [{m.Timestamp:O}] {m.Content[..Math.Min(200, m.Content.Length)]}");
        }
        sb.AppendLine();

        sb.AppendLine("TASK: Provide a structured root cause analysis with the following:");
        sb.AppendLine("1. PROBABLE_ROOT_CAUSE: The most likely root cause (1-2 sentences)");
        sb.AppendLine("2. CONFIDENCE: Low/Medium/High with percentage");
        sb.AppendLine("3. EVIDENCE: List the key evidence supporting this conclusion");
        sb.AppendLine("4. CONTRIBUTING_FACTORS: Other factors that may have contributed");
        sb.AppendLine("5. RECOMMENDED_ACTIONS: Specific, actionable steps to resolve and prevent recurrence");
        sb.AppendLine("6. RELATED_TRACES: IDs of traces that provide the strongest evidence");
        sb.AppendLine();
        sb.AppendLine("Format your response as valid JSON matching this schema:");
        sb.AppendLine("""
                      {
                        "probableRootCause": "string",
                        "confidence": 0.0-1.0,
                        "confidenceLabel": "Low|Medium|High",
                        "evidence": ["string"],
                        "contributingFactors": ["string"],
                        "recommendedActions": [{"action": "string", "priority": "P0|P1|P2", "owner": "string"}],
                        "relatedTraceIds": ["uuid"]
                      }
                      """);

        return sb.ToString();
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private static RootCauseAnalysisResult ParseRCAResponse(string response)
    {
        try
        {
            // Extract JSON from response (may be wrapped in markdown)
            var jsonMatch = Regex.Match(response, @"\{[\s\S]*\}");
            var json = jsonMatch.Success ? jsonMatch.Value : response;

            return JsonSerializer.Deserialize<RootCauseAnalysisResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new RootCauseAnalysisResult(
                "Failed to parse RCA response",
                0.0,
                "Low",
                new List<string> { "Parsing error" },
                new List<string>(),
                new List<RecommendedAction>(),
                new List<string>());
        }
        catch (JsonException ex)
        {
            return new RootCauseAnalysisResult(
                $"Failed to parse RCA: {ex.Message}",
                0.0,
                "Low",
                new List<string> { response[..Math.Min(500, response.Length)] },
                new List<string>(),
                new List<RecommendedAction>(),
                new List<string>());
        }
    }
}
