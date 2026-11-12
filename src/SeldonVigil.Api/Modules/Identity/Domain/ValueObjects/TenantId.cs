namespace SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
    public static TenantId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
