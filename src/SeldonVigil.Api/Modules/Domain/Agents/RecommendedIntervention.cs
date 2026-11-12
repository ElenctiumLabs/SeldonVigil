namespace SeldonVigil.Api.Modules.Domain.Agents;

internal sealed record RecommendedIntervention(
    string Action,
    string Rationale,
    double ExpectedImpact,
    TimeSpan TimeToImplement);
