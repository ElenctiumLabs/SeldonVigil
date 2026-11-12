using SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;

internal sealed record ListCostBudgetsQuery() : IQuery<IReadOnlyList<CostBudgetListItemDto>>;
