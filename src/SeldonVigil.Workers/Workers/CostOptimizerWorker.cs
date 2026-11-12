using SeldonVigil.Api.Modules.CostIntelligence.Domain.Events;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Workers.Workers;

internal sealed partial class CostOptimizerWorker(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider,
    ILogger<CostOptimizerWorker> logger)
    : BackgroundService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Cost Optimizer Worker started")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in cost optimizer loop")]
    private static partial void LogOptimizerLoopError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resetting budget {BudgetId} ({BudgetName}). Previous spend: {PreviousSpend}")]
    private static partial void LogBudgetReset(ILogger logger, CostBudgetId budgetId, string budgetName, decimal previousSpend);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndResetBudgetsAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogOptimizerLoopError(logger, ex);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task CheckAndResetBudgetsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var budgetRepository = scope.ServiceProvider.GetRequiredService<ICostBudgetRepository>();
        var budgets = await budgetRepository.GetAllActiveAsync(ct).ConfigureAwait(false);

        foreach (var budget in budgets)
        {
            if (!budget.ShouldReset(dateTimeProvider))
            {
                continue;
            }

            LogBudgetReset(logger, budget.Id, budget.Name, budget.CurrentSpend.Amount);

            budget.ResetPeriod(dateTimeProvider);
            await budgetRepository.UpdateAsync(budget, ct).ConfigureAwait(false);

            await eventBus.PublishAsync(new BudgetPeriodResetEvent(
                budget.Id,
                budget.TenantId,
                budget.ScopeType,
                budget.ScopeId,
                budget.BudgetAmount,
                dateTimeProvider.UtcNow), ct).ConfigureAwait(false);
        }
    }
}
