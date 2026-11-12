namespace SeldonVigil.Api.Modules.Domain.Agents;

internal sealed record PredictionResponse
{
    public double PredictedValue { get; init; }
    public DateTime PredictionTime { get; init; }
    public double BreachProbability { get; init; }
    public string RiskLevel { get; init; } = "low";
    public IReadOnlyList<RecommendedIntervention> Interventions { get; init; } = [];
    public IReadOnlyList<TimeSeriesPoint> ForecastSeries { get; init; } = [];
}
