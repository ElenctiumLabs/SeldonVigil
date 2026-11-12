namespace SeldonVigil.Api.Modules.Domain.Agents;

internal sealed record RCAResponse
{
    public string ProbableRootCause { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string ConfidenceLabel { get; init; } = "Low";
    public IReadOnlyList<string> Evidence { get; init; } = [];
    public IReadOnlyList<string> ContributingFactors { get; init; } = [];
    public IReadOnlyList<RCAAction> RecommendedActions { get; init; } = [];
    public IReadOnlyList<string> RelatedTraceIds { get; init; } = [];
}
