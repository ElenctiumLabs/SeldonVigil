namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record TrendPointDto(
    DateOnly Date,
    int Count);
