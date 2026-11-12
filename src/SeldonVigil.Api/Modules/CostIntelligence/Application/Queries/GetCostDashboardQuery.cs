using SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;

internal sealed record GetCostDashboardQuery(
    DateTime? From,
    DateTime? To,
    string? GroupBy
) : IQuery<CostDashboardDto>;
