using SeldonVigil.Api.Modules.Domain.Agents;

namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record TimeSeriesForecast(
    IReadOnlyList<TimeSeriesPoint> ForecastSeries,
    double Confidence,
    string Seasonality);
