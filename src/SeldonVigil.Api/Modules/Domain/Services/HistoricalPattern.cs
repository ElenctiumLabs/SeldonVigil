namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record HistoricalPattern(
    string Name,
    string Description,
    double Confidence);
