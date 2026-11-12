using SeldonVigil.Api.Shared.Domain;

namespace SeldonVigil.Api.Modules.AIReasoning.Domain.Entities;

internal sealed class RootCauseAnalysis : BaseEntity
{
    public string TenantId { get; private set; } = string.Empty;
    public string AgentId { get; private set; } = string.Empty;
    public string? TraceId { get; private set; }
    public string AnomalyType { get; private set; } = string.Empty;
    public DateTime AnomalyStartTime { get; private set; }
    public DateTime AnomalyEndTime { get; private set; }
    public string ProbableRootCause { get; private set; } = string.Empty;
    public double Confidence { get; private set; }
    public string ConfidenceLabel { get; private set; } = string.Empty;
    public string EvidenceJson { get; private set; } = "[]";
    public string ContributingFactorsJson { get; private set; } = "[]";
    public string RecommendedActionsJson { get; private set; } = "[]";
    public string RelatedTraceIdsJson { get; private set; } = "[]";
    public string? RawResponse { get; private set; }
    public string Status { get; private set; } = "pending";

    private RootCauseAnalysis() { }

    public static RootCauseAnalysis Create(
        string tenantId,
        string agentId,
        string anomalyType,
        DateTime anomalyStartTime,
        DateTime anomalyEndTime,
        string? traceId = null)
    {
        return new RootCauseAnalysis
        {
            TenantId = tenantId,
            AgentId = agentId,
            TraceId = traceId,
            AnomalyType = anomalyType,
            AnomalyStartTime = anomalyStartTime,
            AnomalyEndTime = anomalyEndTime,
            Status = "processing"
        };
    }

    public void Complete(
        string probableRootCause,
        double confidence,
        string confidenceLabel,
        string evidenceJson,
        string contributingFactorsJson,
        string recommendedActionsJson,
        string relatedTraceIdsJson,
        string? rawResponse = null)
    {
        ProbableRootCause = probableRootCause;
        Confidence = confidence;
        ConfidenceLabel = confidenceLabel;
        EvidenceJson = evidenceJson;
        ContributingFactorsJson = contributingFactorsJson;
        RecommendedActionsJson = recommendedActionsJson;
        RelatedTraceIdsJson = relatedTraceIdsJson;
        RawResponse = rawResponse;
        Status = "completed";
        MarkAsUpdated();
    }

    public void Fail(string error)
    {
        ProbableRootCause = $"Analysis failed: {error}";
        Status = "failed";
        MarkAsUpdated();
    }
}
