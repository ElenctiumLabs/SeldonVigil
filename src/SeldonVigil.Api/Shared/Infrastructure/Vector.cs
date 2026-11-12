namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record Vector
{
    public float[] Data { get; set; } = [];
}
