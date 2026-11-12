using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;

internal sealed record CostDashboardDto(
    Money TotalSpend,
    Money BudgetAmount,
    decimal BudgetUtilizationPercent,
    long TotalTokens,
    int ApiCalls,
    List<CostBreakdownDto> BreakdownByProvider,
    List<CostBreakdownDto> BreakdownByModel,
    List<CostBreakdownDto> BreakdownByAgent,
    List<CostTrendPointDto> DailyTrend);

internal sealed record CostBreakdownDto(
    string Category,
    Money Amount,
    double PercentageOfTotal,
    long Tokens,
    int Calls);
