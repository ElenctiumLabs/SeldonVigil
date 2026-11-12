using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SeldonVigil.Api.Modules.Domain.Interfaces;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// Evaluates reasoning output quality using multiple strategies:
/// - LLM-as-Judge: GPT-4/Claude evaluate coherence, accuracy, actionability
/// - Rule-based: Regex, JSON schema, threshold checks
/// - Regression: Compare against previous versions
/// - Human feedback: Collect and incorporate ratings
/// </summary>
internal sealed partial class EvaluationService(
    IChatClient judgeClient,
    IEvaluationRepository repository,
    ILogger<EvaluationService> logger)
    : IEvaluationService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Evaluation for {PromptName}: {OverallScore:P0} (Passed: {Passed})")]
    private static partial void LogEvaluationCompleted(ILogger logger, string promptName, double overallScore, bool passed);

    [RequiresDynamicCode("Calls SeldonVigil.Api.Modules.Domain.Services.EvaluationService.EvaluateWithLLMAsync<TRequest, TResponse>(TRequest, TResponse, IReadOnlyList<ReasoningStep>, CancellationToken)")]
    [RequiresUnreferencedCode("Calls SeldonVigil.Api.Modules.Domain.Services.EvaluationService.EvaluateWithLLMAsync<TRequest, TResponse>(TRequest, TResponse, IReadOnlyList<ReasoningStep>, CancellationToken)")]
    public async Task<EvaluationResult> EvaluateAsync<TRequest, TResponse>(
        string promptName,
        TRequest request,
        TResponse response,
        IReadOnlyList<ReasoningStep> reasoningSteps,
        CancellationToken ct = default)
    {
        var evaluations = new List<MetricScore>();
        var stopwatch = Stopwatch.StartNew();

        // 1. LLM-as-Judge: Coherence and actionability
        var llmScore = await EvaluateWithLLMAsync(request, response, reasoningSteps, ct)
            .ConfigureAwait(false);
        evaluations.Add(llmScore);

        // 2. Rule-based: Structural validation
        var ruleScore = EvaluateWithRules(response);
        evaluations.Add(ruleScore);

        // 3. Regression check: Compare with previous version
        var regressionScore = await CheckRegressionAsync(promptName, request, response, ct)
            .ConfigureAwait(false);
        evaluations.Add(regressionScore);

        // 4. Cost efficiency: Tokens per unit of output quality
        var costScore = EvaluateCostEfficiency(reasoningSteps);
        evaluations.Add(costScore);

        stopwatch.Stop();

        var overallScore = evaluations.Average(e => e.Score);
        var passed = overallScore >= 0.7 && regressionScore.Score >= 0.5;

        var result = new EvaluationResult
        {
            OverallScore = overallScore,
            Passed = passed,
            Metrics = evaluations.AsReadOnly(),
            EvaluationDurationMs = stopwatch.ElapsedMilliseconds,
            EvaluatedAt = DateTime.UtcNow
        };

        await repository.StoreAsync(promptName, request, response, result, ct)
            .ConfigureAwait(false);

        LogEvaluationCompleted(logger, promptName, overallScore, passed);

        return result;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task<MetricScore> EvaluateWithLLMAsync<TRequest, TResponse>(
        TRequest request,
        TResponse response,
        IReadOnlyList<ReasoningStep> steps,
        CancellationToken ct)
    {
        var evaluationPrompt = $@"
            You are an expert evaluator of AI system outputs. Evaluate the following reasoning output on these dimensions:

            1. COHERENCE (0-1): Is the reasoning logically sound and well-structured?
            2. ACTIONABILITY (0-1): Are the recommended actions specific and implementable?
            3. EVIDENCE_QUALITY (0-1): Is the evidence cited relevant and sufficient?
            4. CALIBRATION (0-1): Is the confidence level appropriate given the evidence?

            REQUEST: {JsonSerializer.Serialize(request)}

            RESPONSE: {JsonSerializer.Serialize(response)}

            REASONING STEPS: {steps.Count} steps, {steps.Sum(s => s.TokenCount)} total tokens

            Respond ONLY with a JSON object in this exact format:
            {{
                ""coherence"": 0.0-1.0,
                ""actionability"": 0.0-1.0,
                ""evidence_quality"": 0.0-1.0,
                ""calibration"": 0.0-1.0,
                ""overall"": 0.0-1.0,
                ""reasoning"": ""brief explanation""
            }}
        ";

        var judgeResponse = await judgeClient.GetResponseAsync(
            new ChatMessage(ChatRole.User, evaluationPrompt),
            new ChatOptions { Temperature = 0.0f, MaxOutputTokens = 500 },
            ct).ConfigureAwait(false);

        try
        {
            var scores = JsonSerializer.Deserialize<LLMEvaluationScores>(
                judgeResponse.Text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new MetricScore(
                "llm_judge",
                scores?.Overall ?? 0.5,
                scores?.Reasoning ?? "Parsing failed");
        }
        catch (JsonException)
        {
            return new MetricScore("llm_judge", 0.5, "Failed to parse LLM evaluation");
        }
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private static MetricScore EvaluateWithRules<TResponse>(TResponse response)
    {
        var score = 1.0;
        var issues = new List<string>();

        // Check for empty or null critical fields
        var properties = typeof(TResponse).GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(response);
            if (value is null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                score -= 0.2;
                issues.Add($"Missing required field: {prop.Name}");
            }
        }

        // Check response length (too short = likely incomplete)
        var json = JsonSerializer.Serialize(response);
        if (json.Length < 100)
        {
            score -= 0.1;
            issues.Add("Response unusually short");
        }

        return new MetricScore(
            "rule_based",
            Math.Max(0, score),
            issues.Count > 0 ? string.Join("; ", issues) : "All checks passed");
    }

    [RequiresDynamicCode("Calls SeldonVigil.Api.Modules.Domain.Services.EvaluationService.EvaluateWithLLMAsync<TRequest, TResponse>(TRequest, TResponse, IReadOnlyList<ReasoningStep>, CancellationToken)")]
    [RequiresUnreferencedCode("Calls SeldonVigil.Api.Modules.Domain.Services.EvaluationService.EvaluateWithLLMAsync<TRequest, TResponse>(TRequest, TResponse, IReadOnlyList<ReasoningStep>, CancellationToken)")]
    private async Task<MetricScore> CheckRegressionAsync<TRequest, TResponse>(
        string promptName,
        TRequest request,
        TResponse response,
        CancellationToken ct)
    {
        var previousEvaluations = await repository.GetRecentEvaluationsAsync(
            promptName, 10, ct).ConfigureAwait(false);

        if (previousEvaluations.Count < 3)
        {
            return new MetricScore("regression", 1.0, "Insufficient history for regression check");
        }

        var currentScore = await EvaluateWithLLMAsync(request, response, [], ct)
            .ConfigureAwait(false);

        var avgPrevious = previousEvaluations.Average(e => e.OverallScore);
        var regression = currentScore.Score - avgPrevious;

        return new MetricScore(
            "regression",
            regression >= -0.1 ? 1.0 : Math.Max(0, 1.0 + regression * 5),
            regression >= -0.1
                ? $"No regression (current: {currentScore.Score:F2}, avg previous: {avgPrevious:F2})"
                : $"Regression detected: {regression:F2} below average");
    }

    private static MetricScore EvaluateCostEfficiency(IReadOnlyList<ReasoningStep> steps)
    {
        var totalTokens = steps.Sum(s => s.TokenCount);
        var totalTime = steps.Sum(s => s.ElapsedMs);

        // Target: < 2000 tokens per reasoning chain, < 5 seconds
        var tokenScore = totalTokens <= 2000 ? 1.0 : Math.Max(0, 1.0 - (totalTokens - 2000) / 8000.0);
        var timeScore = totalTime <= 5000 ? 1.0 : Math.Max(0, 1.0 - (totalTime - 5000) / 15000.0);

        return new MetricScore(
            "cost_efficiency",
            (tokenScore + timeScore) / 2,
            $"{totalTokens} tokens, {totalTime}ms");
    }
}
