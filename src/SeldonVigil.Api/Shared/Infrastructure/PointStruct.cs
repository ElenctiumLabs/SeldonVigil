namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record PointStruct
{
    public PointId Id { get; init; } = new();
    public Vectors Vectors { get; init; } = new();
    public Dictionary<string, Value> Payload { get; init; } = new();
}
