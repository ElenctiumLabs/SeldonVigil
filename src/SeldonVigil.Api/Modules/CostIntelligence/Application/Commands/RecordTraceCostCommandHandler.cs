using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Commands;

internal sealed partial class RecordTraceCostCommandHandler(
    ICostBudgetRepository budgetRepository,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider,
    ILogger<RecordTraceCostCommandHandler> logger)
    : Shared.Application.ICommandHandler<RecordTraceCostCommand>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Budget {BudgetId} period reset. New period started at {ResetAt}")]
    private static partial void LogBudgetPeriodReset(ILogger logger, CostBudgetId budgetId, DateTime resetAt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Budget threshold breached: {BudgetId} at {Threshold:P0}. Current spend: {CurrentSpend:C} / {BudgetAmount:C}")]
    private static partial void LogBudgetThresholdBreached(ILogger logger, CostBudgetId budgetId, decimal threshold, decimal currentSpend, decimal budgetAmount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cost anomaly detected for agent {AgentId}: ${ActualCost:F4} (expected ~${ExpectedCost:F4})")]
    private static partial void LogCostAnomalyDetected(ILogger logger, AgentId agentId, decimal actualCost, decimal expectedCost);

    public async Task<Result<Unit>> Handle(RecordTraceCostCommand request, CancellationToken ct)
    {
        var cost = new Money(request.CostUsd);

        // Find applicable budgets
        var budgets = await budgetRepository.GetApplicableBudgetsAsync(
            request.TenantId,
            request.AgentId,
            ct).ConfigureAwait(false);

        foreach (var budget in budgets)
        {
            if (budget.ShouldReset(dateTimeProvider))
            {
                budget.ResetPeriod(dateTimeProvider);
                LogBudgetPeriodReset(logger, budget.Id, budget.LastResetAt);
            }

            var result = budget.RecordSpend(cost, dateTimeProvider);
            if (result.IsFailure)
            {
                continue;
            }

            var (breached, threshold) = result.Value;
            if (breached && threshold.HasValue)
            {
                LogBudgetThresholdBreached(logger, budget.Id, threshold.Value, budget.CurrentSpend.Amount, budget.BudgetAmount.Amount);

                foreach (var domainEvent in budget.DomainEvents)
                {
                    await eventBus.PublishAsync(domainEvent, ct).ConfigureAwait(false);
                }
                budget.ClearDomainEvents();
            }

            await budgetRepository.UpdateAsync(budget, ct).ConfigureAwait(false);
        }

        // Check for cost anomalies
        await DetectAnomalyAsync(request, ct).ConfigureAwait(false);

        return Result.Success(Unit.Value);
    }

    private async Task DetectAnomalyAsync(RecordTraceCostCommand request, CancellationToken ct)
    {
        // Simple statistical anomaly: cost > 3 standard deviations from 7-day mean
        // In production, this would use a proper time-series model
        var historicalCosts = await budgetRepository.GetRecentCostsAsync(
            request.AgentId, TimeSpan.FromDays(7), ct).ConfigureAwait(false);

        if (historicalCosts.Count < 10)
        {
            return;
        }

        var mean = historicalCosts.Average();
        var stdDev = Math.Sqrt(historicalCosts.Average(c => Math.Pow((double)(c - mean), 2)));

        if (request.CostUsd > mean + (decimal)(3 * stdDev))
        {
            await eventBus.PublishAsync(new CostAnomalyDetectedEvent(
                TraceId: request.TraceId,
                AgentId: request.AgentId,
                ExpectedCost: mean,
                ActualCost: request.CostUsd,
                DeviationPercent: (request.CostUsd - mean) / mean * 100,
                dateTimeProvider.UtcNow), ct).ConfigureAwait(false);

            LogCostAnomalyDetected(logger, request.AgentId, request.CostUsd, mean);
        }
    }
}
