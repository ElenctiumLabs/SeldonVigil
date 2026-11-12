namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record FieldCondition
{
    public string Key { get; init; } = string.Empty;
    public Match Match { get; init; } = new();
}
