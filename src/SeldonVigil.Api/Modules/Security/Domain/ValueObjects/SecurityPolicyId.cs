namespace SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

public readonly record struct SecurityPolicyId(Guid Value)
{
    public static SecurityPolicyId New() => new(Guid.NewGuid());
    public static SecurityPolicyId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
