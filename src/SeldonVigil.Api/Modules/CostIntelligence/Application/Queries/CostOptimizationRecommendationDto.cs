using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;

internal sealed record CostOptimizationRecommendationDto(
    Guid RecommendationId,
    string Type,
    string Title,
    string Description,
    decimal EstimatedMonthlySavingsUsd,
    double Confidence,
    AgentId? AffectedAgentId,
    string? AffectedModel,
    string RecommendedAction,
    DateTime CreatedAt);
