namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record Match
{
    public string Keyword { get; init; } = string.Empty;
}
