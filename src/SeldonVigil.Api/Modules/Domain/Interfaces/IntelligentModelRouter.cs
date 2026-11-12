using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

/// <summary>
/// Routes requests to the optimal model based on task complexity,
/// confidence requirements, and budget constraints.
/// Implements a cascading strategy: cheap → expensive only when needed.
/// </summary>
internal sealed partial class IntelligentModelRouter(ICostTracker costTracker, ILogger<IntelligentModelRouter> logger)
    : IModelRouter
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No model meets requirements: complexity={Complexity}, confidence={Confidence}")]
    private static partial void LogNoModelMeetsRequirements(ILogger logger, TaskComplexity complexity, double confidence);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Selected model {Model} for complexity={Complexity}, confidence={Confidence}")]
    private static partial void LogSelectedModel(ILogger logger, string model, TaskComplexity complexity, double confidence);

    [LoggerMessage(Level = LogLevel.Information, Message = "Model {Model} succeeded for task")]
    private static partial void LogModelSucceeded(ILogger logger, string model);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Model {Model} failed, trying next in cascade")]
    private static partial void LogModelFailed(ILogger logger, Exception exception, string model);

    private readonly Dictionary<string, ModelProfile> _models = new()
    {
        ["claude-3-5-haiku"] = new("claude-3-5-haiku", 0.25, 1.25, 200_000, 0.7, ModelCapability.Basic),
        ["gpt-4o-mini"] = new("gpt-4o-mini", 0.15, 0.60, 128_000, 0.75, ModelCapability.Basic),
        ["gpt-4o"] = new("gpt-4o", 2.50, 10.00, 128_000, 0.88, ModelCapability.Advanced),
        ["claude-3-7-sonnet"] = new("claude-3-7-sonnet", 3.00, 15.00, 200_000, 0.90, ModelCapability.Advanced),
        ["o3-mini"] = new("o3-mini", 1.10, 4.40, 200_000, 0.92, ModelCapability.Reasoning),
        ["claude-3-opus"] = new("claude-3-opus", 15.00, 75.00, 200_000, 0.94, ModelCapability.Reasoning)
    };

    public string SelectModel(
        TaskComplexity complexity,
        double requiredConfidence,
        decimal? budgetLimit,
        string? preferredProvider = null)
    {
        var candidates = _models.Values
            .Where(m => m.Capability >= MapComplexity(complexity))
            .Where(m => m.AccuracyScore >= requiredConfidence)
            .Where(m => preferredProvider is null || m.Name.Contains(preferredProvider, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.EstimatedCostPer1kTokens)
            .ToList();

        if (candidates.Count == 0)
        {
            LogNoModelMeetsRequirements(logger, complexity, requiredConfidence);

            // Fallback to most capable
            return _models.Values.OrderByDescending(m => m.AccuracyScore).First().Name;
        }

        // If budget limit specified, filter further
        if (budgetLimit.HasValue)
        {
            var affordable = candidates
                .Where(m => m.EstimatedCostPer1kTokens * 4 <= (double)budgetLimit.Value) // Assume ~4k tokens
                .ToList();

            if (affordable.Count > 0)
            {
                candidates = affordable;
            }
        }

        var selected = candidates[0];

        LogSelectedModel(logger, selected.Name, complexity, requiredConfidence);

        return selected.Name;
    }

    public async Task<T> RouteWithFallbackAsync<T>(
        TaskComplexity complexity,
        double requiredConfidence,
        Func<string, CancellationToken, Task<T>> executeAsync,
        CancellationToken ct)
    {
        var modelsToTry = GetModelCascade(complexity, requiredConfidence);
        var lastException = new Exception("No models available");

        foreach (var model in modelsToTry)
        {
            try
            {
                var result = await executeAsync(model, ct).ConfigureAwait(false);

                costTracker.TrackUsage(Guid.NewGuid(), 0, 0, model); // Actual tracking in caller

                LogModelSucceeded(logger, model);

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                LogModelFailed(logger, ex, model);
            }
        }

        throw new ModelRoutingException(
            $"All models failed. Last error: {lastException.Message}",
            lastException);
    }

    private IReadOnlyList<string> GetModelCascade(TaskComplexity complexity, double requiredConfidence)
    {
        var capability = MapComplexity(complexity);

        return _models.Values
            .Where(m => m.Capability >= capability)
            .Where(m => m.AccuracyScore >= requiredConfidence * 0.9) // 10% tolerance for fallback
            .OrderBy(m => m.EstimatedCostPer1kTokens)
            .Select(m => m.Name)
            .ToList()
            .AsReadOnly();
    }

    private static ModelCapability MapComplexity(TaskComplexity complexity) => complexity switch
    {
        TaskComplexity.Simple => ModelCapability.Basic,
        TaskComplexity.Standard => ModelCapability.Advanced,
        TaskComplexity.Complex => ModelCapability.Reasoning,
        TaskComplexity.Critical => ModelCapability.Reasoning,
        _ => ModelCapability.Advanced
    };
}
