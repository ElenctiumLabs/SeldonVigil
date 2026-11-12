namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record Filter
{
    public List<Condition> Must { get; init; } = [];
}
