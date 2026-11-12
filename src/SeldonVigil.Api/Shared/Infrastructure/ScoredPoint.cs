namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record ScoredPoint
{
    public PointId Id { get; init; } = new();
    public Dictionary<string, Value> Payload { get; init; } = new();
    public float Score { get; init; }
}
