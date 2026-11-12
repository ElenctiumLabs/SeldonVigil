namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record MetricScore(
    string MetricName,
    double Score,
    string Reasoning);
