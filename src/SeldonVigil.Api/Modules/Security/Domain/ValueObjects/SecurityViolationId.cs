namespace SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

internal readonly record struct SecurityViolationId(Guid Value)
{
    public static SecurityViolationId New() => new(Guid.NewGuid());
    public static SecurityViolationId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
