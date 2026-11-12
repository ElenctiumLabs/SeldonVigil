using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Commands;

internal sealed partial class CreateCostBudgetCommandHandler(
    ICostBudgetRepository budgetRepository,
    ITenantContext tenantContext,
    IDateTimeProvider dateTimeProvider,
    ILogger<CreateCostBudgetCommandHandler> logger)
    : ICommandHandler<CreateCostBudgetCommand, CostBudgetId>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Created cost budget {BudgetId} '{Name}' for scope {ScopeType}/{ScopeId}, amount {Amount} {Currency}")]
    private static partial void LogCostBudgetCreated(ILogger logger, CostBudgetId budgetId, string name, BudgetScopeType scopeType, string? scopeId, decimal amount, string currency);

    public async Task<Shared.Domain.Result<CostBudgetId>> Handle(CreateCostBudgetCommand request, CancellationToken ct)
    {
        var budgetAmount = new Money(request.Amount, request.Currency);

        // Own the budget by the resolved tenant so the global query filter
        // can see it (TenantId.Empty would be invisible to every tenant).
        var tenantId = new TenantId(tenantContext.TenantId ?? System.Guid.Empty);

        var result = CostBudget.Create(
            tenantId,
            request.Name,
            request.ScopeType,
            request.ScopeId,
            budgetAmount,
            request.Period,
            request.AlertThresholds,
            request.EnforcementType,
            Guid.Empty,
            dateTimeProvider);

        if (result.IsFailure)
        {
            return Shared.Domain.Result<CostBudgetId>.Failure(result.Error!);
        }

        var budget = result.Value!;
        try
        {
            await budgetRepository.AddAsync(budget, ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505")
        {
            logger.LogWarning(
                "Duplicate budget rejected for scope {ScopeType}/{ScopeId}, period {Period}",
                request.ScopeType, request.ScopeId.ToString(), request.Period.ToString());

            return Shared.Domain.Result<CostBudgetId>.Failure(Error.Conflict(
                "Budget.Duplicate",
                $"A budget already exists for this scope and period ('{request.Name}')."));
        }

        LogCostBudgetCreated(logger, budget.Id, request.Name, request.ScopeType, request.ScopeId.ToString(), request.Amount, request.Currency);

        return Result.Success(budget.Id);
    }
}
