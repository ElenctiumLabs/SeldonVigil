namespace SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;

public readonly record struct CostBudgetId(Guid Value)
{
    public static CostBudgetId New() => new(Guid.NewGuid());
    public static CostBudgetId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
