using SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;

internal sealed partial class ListCostBudgetsQueryHandler(
    ICostBudgetRepository budgetRepository,
    ILogger<ListCostBudgetsQueryHandler> logger)
    : IQueryHandler<ListCostBudgetsQuery, IReadOnlyList<CostBudgetListItemDto>>
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to list cost budgets")]
    private static partial void LogCostBudgetsListFailed(ILogger logger, Exception exception);

    public async Task<Shared.Domain.Result<IReadOnlyList<CostBudgetListItemDto>>> Handle(
        ListCostBudgetsQuery request, CancellationToken ct)
    {
        try
        {
            var budgets = await budgetRepository.GetAllActiveAsync(ct).ConfigureAwait(false);

            var items = budgets.Select(b => new CostBudgetListItemDto(
                b.Id,
                b.Name,
                b.ScopeType.ToString(),
                b.ScopeId,
                b.BudgetAmount,
                b.CurrentSpend,
                b.BudgetAmount.Amount > 0
                    ? Math.Round(b.CurrentSpend.Amount / b.BudgetAmount.Amount * 100, 2)
                    : 0,
                GetBudgetStatus(b))).ToList();

            return Result.Success<IReadOnlyList<CostBudgetListItemDto>>(items);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCostBudgetsListFailed(logger, ex);
            return Shared.Domain.Result<IReadOnlyList<CostBudgetListItemDto>>.Failure(Error.Internal(
                "Budgets.ListFailed", "Failed to retrieve cost budgets."));
        }
    }

    private static string GetBudgetStatus(CostBudget budget)
    {
        if (budget.CurrentSpend.Amount >= budget.BudgetAmount.Amount)
        {
            return "exceeded";
        }

        var utilization = budget.CurrentSpend.Amount / budget.BudgetAmount.Amount;
        if (budget.AlertThresholds.Count > 0 && utilization >= budget.AlertThresholds.Max())
        {
            return "warning";
        }

        return "active";
    }
}
