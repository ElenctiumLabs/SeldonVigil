namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record PointId
{
    public string Uuid { get; init; } = string.Empty;
}
