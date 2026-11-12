using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence model for a prompt evaluation result.
/// </summary>
internal sealed class EvaluationResultEntity
{
    public Guid Id { get; set; }
    public string PromptName { get; set; } = string.Empty;
    public double OverallScore { get; set; }
    public bool Passed { get; set; }
    public List<MetricScore> Metrics { get; set; } = [];
    public long EvaluationDurationMs { get; set; }
    public DateTime EvaluatedAt { get; set; }
}
