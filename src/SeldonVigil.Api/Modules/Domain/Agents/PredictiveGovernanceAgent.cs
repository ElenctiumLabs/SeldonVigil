using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Agents;

/// <summary>
/// Forecasts policy violations and cost overruns before they occur.
/// Combines time-series analysis with LLM reasoning for actionable predictions.
/// </summary>
internal sealed class PredictiveGovernanceAgent(
    IChatClient chatClient,
    IToolOrchestrator toolOrchestrator,
    ToolFunctionAdapter toolFunctionAdapter,
    IMemoryService memoryService,
    IPromptManager promptManager,
    IEvaluationService evaluationService,
    ICostTracker costTracker,
    ITimeSeriesAnalyzer timeSeriesAnalyzer,
    ILogger<PredictiveGovernanceAgent> logger)
    : BaseReasoningAgent<PredictionRequest, PredictionResponse>(chatClient, toolOrchestrator, toolFunctionAdapter, memoryService,
        promptManager, evaluationService, costTracker, logger)
{
    protected override string GetPromptName() => "predictive_governance";

    protected override Dictionary<string, object> BuildPromptVariables(PredictionRequest request, MemoryContext memory)
    {
        var forecast = timeSeriesAnalyzer.Forecast(
            request.HistoricalData,
            request.Horizon,
            request.ConfidenceLevel);

        return new Dictionary<string, object>
        {
            ["forecast"] = forecast,
            ["target_metric"] = request.TargetMetric,
            ["threshold"] = request.Threshold,
            ["current_value"] = request.CurrentValue,
            ["historical_patterns"] = memory.Patterns,
            ["seasonality"] = forecast.Seasonality
        };
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    protected override PredictionResponse ParseResponse(string responseText)
    {
        var json = Regex.Match(responseText, @"\{[\s\S]*\}").Value;
        return JsonSerializer.Deserialize<PredictionResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new PredictionResponse();
    }

    protected override Task<MemoryContext> RetrieveMemoryAsync(
        PredictionRequest request, ReasoningContext context, CancellationToken ct)
    {
        // Predictive governance uses primarily time-series data, not semantic memory
        return Task.FromResult(new MemoryContext(
            [],
            [],
            new Dictionary<string, object>()));
    }

    protected override Task StoreMemoryAsync(
        PredictionRequest request,
        PredictionResponse? response,
        List<ReasoningStep> steps,
        List<ToolCallRecord> toolCalls,
        ReasoningContext context,
        CancellationToken ct)
    {
        // Predictions stored as structured events for accuracy tracking
        return Task.CompletedTask;
    }
}
