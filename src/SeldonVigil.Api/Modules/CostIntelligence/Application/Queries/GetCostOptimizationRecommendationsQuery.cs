using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;

internal sealed record GetCostOptimizationRecommendationsQuery(
    AgentId? AgentId = null,
    int Limit = 10
) : IQuery<IReadOnlyList<CostOptimizationRecommendationDto>>;
