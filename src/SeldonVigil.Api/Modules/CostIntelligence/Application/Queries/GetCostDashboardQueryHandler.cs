using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;

/// <summary>
/// Builds the cost dashboard from two sources:
/// - Budgets: limits, utilization and threshold state (CostIntelligence store).
/// - Traces:  real spend, token usage, call counts and daily trend
///            (Observability store - read directly, consistent with the
///            recommendations handler precedent).
/// </summary>
internal sealed partial class GetCostDashboardQueryHandler(
    ICostBudgetRepository budgetRepository,
    SeldonVigilDbContext observabilityDb,
    ILogger<GetCostDashboardQueryHandler> logger)
    : IQueryHandler<GetCostDashboardQuery, CostDashboardDto>
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to retrieve cost dashboard")]
    private static partial void LogCostDashboardFailed(ILogger logger, Exception exception);

    public async Task<Shared.Domain.Result<CostDashboardDto>> Handle(GetCostDashboardQuery request, CancellationToken ct)
    {
        try
        {
            // ── Real usage from ingested traces ────────────────────────────
            var from = request.From ?? DateTime.UtcNow.Date.AddDays(-30);

            var usage = await observabilityDb.Traces
                .AsNoTracking()
                .Where(t => t.CreatedAt >= from)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalCost = g.Sum(t => t.CostUsd),
                    InputTokens = g.Sum(t => t.TokenUsageInput),
                    OutputTokens = g.Sum(t => t.TokenUsageOutput),
                    Calls = g.Count(),
                })
                .SingleOrDefaultAsync(ct).ConfigureAwait(false);

            var totalCostUsd = usage?.TotalCost ?? 0m;
            var totalTokens = (usage?.InputTokens ?? 0) + (usage?.OutputTokens ?? 0);
            var apiCalls = usage?.Calls ?? 0;

            var byAgent = await observabilityDb.Traces
                .AsNoTracking()
                .Where(t => t.CreatedAt >= from)
                .GroupBy(t => t.AgentId)
                .Select(g => new
                {
                    AgentId = g.Key,
                    Cost = g.Sum(t => t.CostUsd),
                    Tokens = g.Sum(t => t.TokenUsageInput) + g.Sum(t => t.TokenUsageOutput),
                    Calls = g.Count(),
                })
                .ToListAsync(ct).ConfigureAwait(false);

            var byModel = await observabilityDb.Traces
                .AsNoTracking()
                .Where(t => t.CreatedAt >= from && t.ModelName != "unknown")
                .GroupBy(t => t.ModelName)
                .Select(g => new
                {
                    Model = g.Key,
                    Cost = g.Sum(t => t.CostUsd),
                    Tokens = g.Sum(t => t.TokenUsageInput) + g.Sum(t => t.TokenUsageOutput),
                    Calls = g.Count(),
                })
                .ToListAsync(ct).ConfigureAwait(false);

            var daily = await observabilityDb.Traces
                .AsNoTracking()
                .Where(t => t.CreatedAt >= from)
                .GroupBy(t => t.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, Cost = g.Sum(t => t.CostUsd) })
                .ToListAsync(ct).ConfigureAwait(false);

            // ── Budgets: limits + utilization ──────────────────────────────
            var budgets = await budgetRepository.GetAllActiveAsync(ct).ConfigureAwait(false);
            var totalBudget = budgets.Aggregate(new Money(0, "USD"), (acc, b) => acc.Add(b.BudgetAmount));
            var utilization = totalBudget.Amount > 0 ? totalCostUsd / totalBudget.Amount * 100 : 0;

            double Pct(decimal amount) => totalCostUsd > 0 ? (double)(amount / totalCostUsd * 100) : 0;

            var breakdownByAgent = byAgent
                .OrderByDescending(a => a.Cost)
                .Select(a => new CostBreakdownDto(
                    a.AgentId.ToString(),
                    new Money(a.Cost, "USD"),
                    Pct(a.Cost),
                    a.Tokens,
                    a.Calls))
                .ToList();

            var breakdownByModel = byModel
                .OrderByDescending(m => m.Cost)
                .Select(m => new CostBreakdownDto(
                    m.Model,
                    new Money(m.Cost, "USD"),
                    Pct(m.Cost),
                    m.Tokens,
                    m.Calls))
                .ToList();

            List<CostBreakdownDto> breakdownByProvider = byModel.Count > 0
                ? [new("Custom / Local", new Money(byModel.Sum(m => m.Cost), "USD"), 100,
                       byModel.Sum(m => m.Tokens), byModel.Sum(m => m.Calls))]
                : [];

            var dailyTrend = daily
                .Select(d => new CostTrendPointDto(d.Date, new Money(d.Cost, "USD")))
                .ToList();

            var dashboard = new CostDashboardDto(
                new Money(totalCostUsd, "USD"),
                totalBudget,
                Math.Round(utilization, 2),
                totalTokens,
                apiCalls,
                breakdownByProvider,
                breakdownByModel,
                breakdownByAgent,
                dailyTrend);

            return Shared.Domain.Result<CostDashboardDto>.Success(dashboard);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCostDashboardFailed(logger, ex);
            return Shared.Domain.Result<CostDashboardDto>.Failure(Error.Internal(
                "Dashboard.RetrievalFailed", "Failed to retrieve cost dashboard data."));
        }
    }
}
