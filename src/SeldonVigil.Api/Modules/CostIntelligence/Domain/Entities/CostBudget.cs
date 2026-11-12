using SeldonVigil.Api.Modules.CostIntelligence.Domain.Events;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;

public sealed class CostBudget : BaseAggregateRoot<CostBudgetId>
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public BudgetScopeType ScopeType { get; private set; }
    public Guid ScopeId { get; private set; }
    public Money BudgetAmount { get; private set; } = null!;
    public BudgetPeriod Period { get; private set; }
    public IReadOnlyList<decimal> AlertThresholds { get; private set; } = [];
    public EnforcementType EnforcementType { get; private set; }
    public Money CurrentSpend { get; private set; } = null!;
    public DateTime LastResetAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private CostBudget() { }

    public static Result<CostBudget> Create(
        TenantId tenantId,
        string name,
        BudgetScopeType scopeType,
        Guid scopeId,
        Money budgetAmount,
        BudgetPeriod period,
        List<decimal> alertThresholds,
        EnforcementType enforcementType,
        Guid createdBy,
        IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(budgetAmount);
        ArgumentNullException.ThrowIfNull(alertThresholds);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            return Result<CostBudget>.Failure(Error.Validation(
                "Budget.InvalidName", "Budget name is required and must not exceed 255 characters."));
        }

        if (budgetAmount.Amount <= 0)
        {
            return Result<CostBudget>.Failure(Error.Validation(
                "Budget.InvalidAmount", "Budget amount must be greater than zero."));
        }

        if (alertThresholds.Any(t => t <= 0 || t > 1))
        {
            return Result<CostBudget>.Failure(Error.Validation(
                "Budget.InvalidThresholds", "Alert thresholds must be between 0 and 1."));
        }

        var budget = new CostBudget
        {
            Id = CostBudgetId.New(),
            TenantId = tenantId,
            Name = name.Trim(),
            ScopeType = scopeType,
            ScopeId = scopeId,
            BudgetAmount = budgetAmount,
            Period = period,
            AlertThresholds = alertThresholds.AsReadOnly(),
            EnforcementType = enforcementType,
            CurrentSpend = new Money(0, budgetAmount.Currency),
            LastResetAt = dateTimeProvider.UtcNow,
            CreatedBy = createdBy,
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = dateTimeProvider.UtcNow
        };

        return Result<CostBudget>.Success(budget);
    }

    public Result<(bool ThresholdBreached, decimal? BreachedThreshold)> RecordSpend(
        Money amount, IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        if (amount.Currency != CurrentSpend.Currency)
        {
            return Result<(bool, decimal?)>.Failure(Error.Validation(
                "Budget.CurrencyMismatch",
                $"Cannot record spend in {amount.Currency}. Budget currency is {CurrentSpend.Currency}."));
        }

        CurrentSpend = new Money(CurrentSpend.Amount + amount.Amount, CurrentSpend.Currency);
        UpdatedAt = dateTimeProvider.UtcNow;

        var utilization = CurrentSpend.Amount / BudgetAmount.Amount;
        var breachedThreshold = AlertThresholds
            .OrderByDescending(t => t)
            .FirstOrDefault(t => utilization >= t);

        var wasPreviouslyBreached = false; // Would track state in production

        if (breachedThreshold > 0 && !wasPreviouslyBreached)
        {
            AddDomainEvent(new BudgetThresholdBreachedEvent(
                Id,
                TenantId,
                ScopeType,
                ScopeId,
                BudgetAmount,
                CurrentSpend,
                breachedThreshold,
                dateTimeProvider.UtcNow));

            if (EnforcementType == EnforcementType.Hard && utilization >= 1.0m)
            {
                AddDomainEvent(new BudgetLimitEnforcedEvent(
                    Id,
                    TenantId,
                    ScopeType,
                    ScopeId,
                    dateTimeProvider.UtcNow));
            }

            return Result<(bool, decimal?)>.Success((true, breachedThreshold));
        }

        return Result<(bool, decimal?)>.Success((false, null));
    }

    public void ResetPeriod(IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        CurrentSpend = new Money(0, BudgetAmount.Currency);
        LastResetAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    public bool ShouldReset(IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        var now = dateTimeProvider.UtcNow;
        return Period switch
        {
            BudgetPeriod.Daily => (now - LastResetAt).TotalDays >= 1,
            BudgetPeriod.Weekly => (now - LastResetAt).TotalDays >= 7,
            BudgetPeriod.Monthly => (now - LastResetAt).TotalDays >= 30,
            BudgetPeriod.Quarterly => (now - LastResetAt).TotalDays >= 90,
            BudgetPeriod.Annual => (now - LastResetAt).TotalDays >= 365,
            _ => false
        };
    }
}
