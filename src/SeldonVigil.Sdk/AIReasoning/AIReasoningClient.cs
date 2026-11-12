using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.AIReasoning;

/// <summary>
/// Client for AI reasoning operations (root cause analysis).
/// </summary>
public class AIReasoningClient
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIReasoningClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to communicate with the SeldonVigil API.</param>
    /// <param name="authClient">The authentication client used to acquire bearer access tokens.</param>
    /// <param name="options">The configuration options providing the base URL and API version.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/>, <paramref name="authClient"/>, or <paramref name="options"/> is <see langword="null"/>.</exception>
    public AIReasoningClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Triggers AI-powered root cause analysis for an anomaly.
    /// </summary>
    /// <param name="request">The request that describes the anomaly to analyze.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>The root cause analysis produced by the AI reasoning service.</returns>
    public async Task<RootCauseAnalysis> TriggerRootCauseAnalysisAsync(TriggerRcaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/ai-reasoning/root-cause-analysis", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<RootCauseAnalysisWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to trigger RCA.") : MapRca(wire);
    }

    /// <summary>
    /// Gets a root cause analysis by ID.
    /// </summary>
    /// <param name="rcaId">The identifier of the root cause analysis to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>The requested root cause analysis, or <see langword="null"/> if no analysis exists with the specified identifier.</returns>
    public async Task<RootCauseAnalysis?> GetRootCauseAnalysisAsync(string rcaId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/ai-reasoning/root-cause-analysis/{rcaId}", null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        var wire = await response.Content.ReadFromJsonAsync<RootCauseAnalysisWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? null : MapRca(wire);
    }

    /// <summary>
    /// Gets root cause analyses for a specific agent.
    /// </summary>
    /// <param name="agentId">The identifier of the agent whose root cause analyses are to be retrieved.</param>
    /// <param name="limit">The maximum number of analyses to return.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>The root cause analyses associated with the specified agent.</returns>
    public async Task<List<RootCauseAnalysis>> GetByAgentAsync(string agentId, int limit = 50, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/ai-reasoning/root-cause-analysis/agent/{agentId}?limit={limit}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<RootCauseAnalysisWire>>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Select(MapRca).ToList() ?? [];
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        var token = await _authClient.GetAccessTokenAsync(ct).ConfigureAwait(false);

        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: SdkJson.Options);
        }

        return await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    private static RootCauseAnalysis MapRca(RootCauseAnalysisWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        TenantId = wire.TenantId ?? string.Empty,
        AgentId = wire.AgentId ?? string.Empty,
        TraceId = wire.TraceId,
        AnomalyType = wire.AnomalyType ?? string.Empty,
        AnomalyStartTime = wire.AnomalyStartTime,
        AnomalyEndTime = wire.AnomalyEndTime,
        ProbableRootCause = wire.ProbableRootCause ?? string.Empty,
        Confidence = wire.Confidence,
        ConfidenceLabel = wire.ConfidenceLabel ?? string.Empty,
        Evidence = wire.Evidence ?? [],
        ContributingFactors = wire.ContributingFactors ?? [],
        RecommendedActions = wire.RecommendedActions?.Select(a => new RecommendedAction
        {
            Action = a.Action ?? string.Empty,
            Priority = a.Priority ?? string.Empty,
            Owner = a.Owner ?? string.Empty
        }).ToList() ?? [],
        RelatedTraceIds = wire.RelatedTraceIds ?? [],
        Status = wire.Status ?? string.Empty,
        CreatedAt = wire.CreatedAt
    };
}

/// <summary>
/// Describes an anomaly for which AI-powered root cause analysis should be triggered.
/// </summary>
public class TriggerRcaRequest
{
    /// <summary>
    /// The identifier of the agent associated with the anomaly.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    /// <summary>
    /// The type of anomaly that was detected.
    /// </summary>
    public string AnomalyType { get; set; } = string.Empty;
    /// <summary>
    /// The start time of the anomaly window.
    /// </summary>
    public DateTime AnomalyStartTime { get; set; }
    /// <summary>
    /// The end time of the anomaly window.
    /// </summary>
    public DateTime AnomalyEndTime { get; set; }
    /// <summary>
    /// The optional trace identifier associated with the anomaly.
    /// </summary>
    public string? TraceId { get; set; }
}

/// <summary>
/// Represents the result of an AI-powered root cause analysis.
/// </summary>
public class RootCauseAnalysis
{
    /// <summary>
    /// The unique identifier of the root cause analysis.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// The identifier of the tenant that owns the analysis.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// The identifier of the agent associated with the anomaly.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    /// <summary>
    /// The trace identifier associated with the anomaly, if available.
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// The type of anomaly that was analyzed.
    /// </summary>
    public string AnomalyType { get; set; } = string.Empty;
    /// <summary>
    /// The start time of the anomaly window.
    /// </summary>
    public DateTime AnomalyStartTime { get; set; }
    /// <summary>
    /// The end time of the anomaly window.
    /// </summary>
    public DateTime AnomalyEndTime { get; set; }
    /// <summary>
    /// The probable root cause determined by the AI reasoning service.
    /// </summary>
    public string ProbableRootCause { get; set; } = string.Empty;
    /// <summary>
    /// The confidence score of the analysis, typically between 0 and 1.
    /// </summary>
    public double Confidence { get; set; }
    /// <summary>
    /// A human-readable label describing the confidence level.
    /// </summary>
    public string ConfidenceLabel { get; set; } = string.Empty;
    /// <summary>
    /// The evidence supporting the root cause determination.
    /// </summary>
    public List<string> Evidence { get; set; } = [];
    /// <summary>
    /// The factors that contributed to the anomaly.
    /// </summary>
    public List<string> ContributingFactors { get; set; } = [];
    /// <summary>
    /// The actions recommended to remediate the root cause.
    /// </summary>
    public List<RecommendedAction> RecommendedActions { get; set; } = [];
    /// <summary>
    /// The identifiers of traces related to the anomaly.
    /// </summary>
    public List<string> RelatedTraceIds { get; set; } = [];
    /// <summary>
    /// The current status of the analysis.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// The UTC timestamp at which the analysis was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Describes a remediation action recommended for a root cause.
/// </summary>
public class RecommendedAction
{
    /// <summary>
    /// The remediation action to perform.
    /// </summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// The priority of the action.
    /// </summary>
    public string Priority { get; set; } = string.Empty;
    /// <summary>
    /// The owner responsible for performing the action.
    /// </summary>
    public string Owner { get; set; } = string.Empty;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class RootCauseAnalysisWire
{
    public string? Id { get; set; }
    public string? TenantId { get; set; }
    public string? AgentId { get; set; }
    public string? TraceId { get; set; }
    public string? AnomalyType { get; set; }
    public DateTime AnomalyStartTime { get; set; }
    public DateTime AnomalyEndTime { get; set; }
    public string? ProbableRootCause { get; set; }
    public double Confidence { get; set; }
    public string? ConfidenceLabel { get; set; }
    public List<string>? Evidence { get; set; }
    public List<string>? ContributingFactors { get; set; }
    public List<RecommendedActionWire>? RecommendedActions { get; set; }
    public List<string>? RelatedTraceIds { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class RecommendedActionWire
{
    public string? Action { get; set; }
    public string? Priority { get; set; }
    public string? Owner { get; set; }
}
