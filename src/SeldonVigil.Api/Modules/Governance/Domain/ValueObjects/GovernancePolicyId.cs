namespace SeldonVigil.Api.Modules.Governance.Domain.ValueObjects;

internal readonly record struct GovernancePolicyId(Guid Value)
{
    public static GovernancePolicyId New() => new(Guid.NewGuid());
    public static GovernancePolicyId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
