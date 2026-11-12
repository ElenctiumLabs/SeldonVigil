namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record EvaluationResult
{
    public double OverallScore { get; init; }
    public bool Passed { get; init; }
    public IReadOnlyList<MetricScore> Metrics { get; init; } = [];
    public long EvaluationDurationMs { get; init; }
    public DateTime EvaluatedAt { get; init; }
}
