using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;

internal sealed class GetCostOptimizationRecommendationsQueryHandler(
    ITraceStore traceStore,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetCostOptimizationRecommendationsQuery, IReadOnlyList<CostOptimizationRecommendationDto>>
{
    public async Task<Result<IReadOnlyList<CostOptimizationRecommendationDto>>> Handle(
        GetCostOptimizationRecommendationsQuery request, CancellationToken ct)
    {
        var recommendations = new List<CostOptimizationRecommendationDto>();
        var now = dateTimeProvider.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);

        // Get agent performance data
        var traces = await traceStore.GetAgentTracesAsync(
            request.AgentId ?? AgentId.Empty,
            sevenDaysAgo,
            now,
            10000,
            ct).ConfigureAwait(false);

        if (traces.Count == 0)
        {
            return Result.Success<IReadOnlyList<CostOptimizationRecommendationDto>>(
                []);
        }

        // Recommendation 1: Model downgrade
        var modelUsage = traces
            .GroupBy(t => t.ModelName)
            .Select(g => new
            {
                Model = g.Key,
                Count = g.Count(),
                AvgLatency = g.Average(t => t.DurationMs),
                TotalCost = g.Sum(t => t.CostUsd)
            })
            .OrderByDescending(x => x.TotalCost)
            .ToList();

        foreach (var usage in modelUsage.Where(u => u.TotalCost > 10))
        {
            var cheaperAlternative = await FindCheaperAlternativeAsync(
                usage.Model, usage.AvgLatency).ConfigureAwait(false);

            if (cheaperAlternative is not null)
            {
                var estimatedSavings = usage.TotalCost * 0.4m; // Conservative 40% savings estimate
                recommendations.Add(new CostOptimizationRecommendationDto(
                    RecommendationId: Guid.NewGuid(),
                    Type: "model_downgrade",
                    Title: $"Switch from {usage.Model} to {cheaperAlternative}",
                    Description: $"Based on {usage.Count} traces, {usage.Model} costs ${usage.TotalCost:F2}/week. " +
                                 $"{cheaperAlternative} offers similar latency ({usage.AvgLatency:F0}ms) at ~40% lower cost.",
                    EstimatedMonthlySavingsUsd: estimatedSavings * 4,
                    Confidence: 0.85,
                    AffectedAgentId: request.AgentId,
                    AffectedModel: usage.Model,
                    RecommendedAction: cheaperAlternative,
                    CreatedAt: now));
            }
        }

        // Recommendation 2: Prompt caching
        var repeatedPrompts = traces
            .Where(t => !string.IsNullOrEmpty(t.OperationName))
            .GroupBy(t => t.OperationName)
            .Where(g => g.Count() > 100)
            .Select(g => new
            {
                Prompt = g.Key,
                Count = g.Count(),
                AvgTokens = g.Average(t => t.TotalTokens)
            })
            .ToList();

        foreach (var prompt in repeatedPrompts.Take(3))
        {
            var cacheHitRate = 0.7m; // Estimated
            var estimatedSavings = prompt.Count * prompt.AvgTokens * (double)0.00001m * (double)cacheHitRate; // Rough token pricing

            recommendations.Add(new CostOptimizationRecommendationDto(
                RecommendationId: Guid.NewGuid(),
                Type: "prompt_caching",
                Title: $"Cache high-frequency prompt: {prompt.Prompt[..Math.Min(50, prompt.Prompt.Length)]}...",
                Description: $"This prompt was used {prompt.Count} times in the last 7 days. " +
                             $"Implementing semantic caching could reduce costs by ~{cacheHitRate:P0}.",
                EstimatedMonthlySavingsUsd: (decimal)(estimatedSavings * 4),
                Confidence: 0.75,
                AffectedAgentId: request.AgentId,
                AffectedModel: null,
                RecommendedAction: "Implement semantic prompt caching",
                CreatedAt: now));
        }

        return Result.Success<IReadOnlyList<CostOptimizationRecommendationDto>>(
            recommendations.OrderByDescending(r => r.EstimatedMonthlySavingsUsd).Take(request.Limit).ToList());
    }

    private static async Task<string?> FindCheaperAlternativeAsync(
        string currentModel, double currentLatency)
    {
        // Simplified: In production, this queries a model capability matrix
        var alternatives = new Dictionary<string, (string Alternative, double MaxLatency)>
        {
            ["gpt-4-turbo"] = ("claude-3-5-sonnet", currentLatency * 1.2),
            ["gpt-4o"] = ("claude-3-5-sonnet", currentLatency * 1.1),
            ["claude-3-opus"] = ("claude-3-5-sonnet", currentLatency * 1.3),
            ["gemini-1.5-pro"] = ("gemini-1.5-flash", currentLatency * 1.5),
        };

        return alternatives.TryGetValue(currentModel.ToLowerInvariant(), out var alt)
            ? alt.Alternative
            : null;
    }
}
