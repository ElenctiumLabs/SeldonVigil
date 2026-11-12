namespace SeldonVigil.Api.Modules.Domain.Agents;

internal sealed record PredictionRequest(
    string TargetMetric,
    double Threshold,
    double CurrentValue,
    IReadOnlyList<TimeSeriesPoint> HistoricalData,
    int Horizon, // hours ahead
    double ConfidenceLevel);
