using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.Observability;

/// <summary>
/// Client for observability operations in the SeldonVigil platform.
/// </summary>
public class ObservabilityClient
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservabilityClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="authClient">The authentication client.</param>
    /// <param name="options">The configuration options.</param>
    public ObservabilityClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Ingests a trace into the SeldonVigil platform.
    /// </summary>
    /// <param name="trace">The trace to ingest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ingested trace ID.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when trace is null.</exception>
    public async Task<string> IngestTraceAsync(SeldonVigilTrace trace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/observability/traces", trace, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TraceIngestResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.TraceId ?? throw new InvalidOperationException("Failed to ingest trace.");
    }

    /// <summary>
    /// Ingests multiple traces into the SeldonVigil platform in a single HTTP call.
    /// </summary>
    /// <param name="traces">The traces to ingest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of ingested trace IDs.</returns>
    public async Task<List<string>> IngestTracesAsync(List<SeldonVigilTrace> traces, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(traces);

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/observability/traces/batch", traces, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TracesBatchIngestResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.TraceIds ?? [];
    }

    /// <summary>
    /// Gets a trace by ID.
    /// </summary>
    /// <param name="traceId">The trace ID (GUID).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The trace, or null if not found.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when trace ID is not a valid GUID.</exception>
    public async Task<SeldonVigilTrace?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(traceId, out var guid))
        {
            throw new ArgumentException("Trace ID must be a valid GUID.", nameof(traceId));
        }

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/observability/traces/{guid}", null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var wire = await response.Content.ReadFromJsonAsync<TraceDtoWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? null : MapTrace(wire);
    }

    /// <summary>
    /// Queries traces with optional filters. The API returns a flat array of
    /// trace summaries (id, agentId, status, createdAt, durationMs, error, costUsd).
    /// </summary>
    /// <param name="query">The trace query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of traces.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public async Task<List<SeldonVigilTrace>> QueryTracesAsync(TraceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/observability/traces{BuildQueryString(query)}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<TraceDtoWire>>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Select(MapTrace).ToList() ?? [];
    }

    private static SeldonVigilTrace MapTrace(TraceDtoWire wire) => new()
    {
        Id = wire.Id,
        AgentId = wire.AgentId,
        AgentName = wire.AgentName,
        ParentTraceId = wire.ParentTraceId,
        ModelName = wire.ModelName,
        OperationName = !string.IsNullOrWhiteSpace(wire.OperationName) ? wire.OperationName : (wire.Error ?? "unknown"),
        OperationType = wire.OperationType ?? "custom",
        Status = Enum.TryParse<TraceStatus>(wire.Status, ignoreCase: true, out var status) ? status : TraceStatus.Ok,
        StartedAt = wire.CreatedAt,
        DurationMs = wire.DurationMs,
        Cost = wire.CostUsd,
        Attributes = wire.Attributes?.ToDictionary(k => k.Key, v => (object)v.Value) ?? new(),
        Events = wire.Events?.Select(e => new TraceEvent { Name = e.Name, Timestamp = wire.CreatedAt, Attributes = new() { ["value"] = e.Value } }).ToList() ?? new(),
        TokenUsage = wire.TotalTokens.HasValue ? new TokenUsage { InputTokens = wire.TotalTokens.Value, OutputTokens = 0 } : null,
        Children = wire.Children?.Select(MapTrace).ToList() ?? new(),
    };

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

    private static string BuildQueryString(TraceQuery query)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.AgentId))
        {
            parameters.Add($"agentId={Uri.EscapeDataString(query.AgentId)}");
        }

        if (!string.IsNullOrWhiteSpace(query.OperationName))
        {
            parameters.Add($"operationName={Uri.EscapeDataString(query.OperationName)}");
        }

        if (query.Status.HasValue)
        {
            parameters.Add($"status={query.Status}");
        }

        if (query.StartTime.HasValue)
        {
            parameters.Add($"from={Uri.EscapeDataString(query.StartTime.Value.ToString("O"))}");
        }

        if (query.EndTime.HasValue)
        {
            parameters.Add($"to={Uri.EscapeDataString(query.EndTime.Value.ToString("O"))}");
        }

        if (query.Limit.HasValue)
        {
            parameters.Add($"limit={query.Limit.Value}");
        }

        return parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : string.Empty;
    }
}

/// <summary>
/// Query parameters for trace searches.
/// </summary>
public class TraceQuery
{
    /// <summary>
    /// Gets or sets the agent ID filter.
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Gets or sets the operation name filter.
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public TraceStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the start time filter.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time filter.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int? Limit { get; set; }
}

/// <summary>
/// Response from trace ingestion.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class TraceIngestResponse
{
    public string TraceId { get; set; } = string.Empty;
}

/// <summary>
/// Response from batch trace ingestion.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class TracesBatchIngestResponse
{
    public List<string> TraceIds { get; set; } = [];
}

/// <summary>
/// Trace summary as returned by the API list/get endpoints.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class TraceDtoWire
{
    public string Id { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string? AgentName { get; set; }
    public string? ParentTraceId { get; set; }
    public string? OperationName { get; set; }
    public string? OperationType { get; set; }
    public string? ModelName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public long DurationMs { get; set; }
    public string? Error { get; set; }
    public decimal? CostUsd { get; set; }
    public int? TotalTokens { get; set; }
    public string? StatusMessage { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
    public List<TraceEventDtoWire>? Events { get; set; }
    public List<TraceDtoWire>? Children { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class TraceEventDtoWire
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
