using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;

internal sealed record CostTrendPointDto(
    DateTime Date,
    Money Amount);
