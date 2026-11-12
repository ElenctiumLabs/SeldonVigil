namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record Condition
{
    public FieldCondition Field { get; init; } = new();
}
