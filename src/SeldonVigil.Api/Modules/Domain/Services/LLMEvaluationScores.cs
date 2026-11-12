namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record LLMEvaluationScores
{
    public double Coherence { get; init; }
    public double Actionability { get; init; }
    public double EvidenceQuality { get; init; }
    public double Calibration { get; init; }
    public double Overall { get; init; }
    public string Reasoning { get; init; } = string.Empty;
}
