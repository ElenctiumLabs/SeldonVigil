namespace SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;

public readonly record struct TraceId(Guid Value)
{
    public static TraceId New() => new(Guid.NewGuid());
    public static TraceId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
