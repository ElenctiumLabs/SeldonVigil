namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record Vectors
{
    public Vector Vector { get; init; } = new();
}
