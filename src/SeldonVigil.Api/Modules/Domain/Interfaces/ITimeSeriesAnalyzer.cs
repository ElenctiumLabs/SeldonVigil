using SeldonVigil.Api.Modules.Domain.Agents;
using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface ITimeSeriesAnalyzer
{
    TimeSeriesForecast Forecast(IReadOnlyList<TimeSeriesPoint> data, int horizon, double confidenceLevel);
}
