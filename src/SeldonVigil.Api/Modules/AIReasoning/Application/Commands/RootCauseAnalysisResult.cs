namespace SeldonVigil.Api.Modules.AIReasoning.Application.Commands;

internal sealed record RootCauseAnalysisResult(
    string ProbableRootCause,
    double Confidence,
    string ConfidenceLabel,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ContributingFactors,
    IReadOnlyList<RecommendedAction> RecommendedActions,
    IReadOnlyList<string> RelatedTraceIds);
