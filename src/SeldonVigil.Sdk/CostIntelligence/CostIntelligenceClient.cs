using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.CostIntelligence;

/// <summary>
/// Client for cost intelligence operations in the SeldonVigil platform.
/// </summary>
public class CostIntelligenceClient
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CostIntelligenceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send requests.</param>
    /// <param name="authClient">The authentication client used to obtain access tokens.</param>
    /// <param name="options">The SDK configuration options.</param>
    public CostIntelligenceClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Gets the cost dashboard with trends and breakdowns.
    /// </summary>
    public async Task<CostDashboard> GetDashboardAsync(DateTime? from = null, DateTime? to = null, string? groupBy = null, CancellationToken cancellationToken = default)
    {
        var query = BuildDashboardQueryString(from, to, groupBy);
        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/costs/dashboard{query}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CostDashboardWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return MapDashboard(result);
    }

    /// <summary>
    /// Gets cost optimization recommendations.
    /// </summary>
    public async Task<List<CostRecommendation>> GetRecommendationsAsync(string? agentId = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        var query = $"?limit={limit}";
        if (!string.IsNullOrWhiteSpace(agentId))
        {
            query += $"&agentId={Uri.EscapeDataString(agentId)}";
        }

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/costs/recommendations{query}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<CostRecommendationWire>>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Select(MapRecommendation).ToList() ?? [];
    }

    /// <summary>
    /// Lists all cost budgets.
    /// </summary>
    public async Task<List<CostBudget>> ListBudgetsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/costs/budgets", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<CostBudgetWire>>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Select(MapBudget).ToList() ?? [];
    }

    /// <summary>
    /// Creates a new cost budget.
    /// </summary>
    public async Task<string> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/costs/budgets", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateBudgetResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Id ?? throw new InvalidOperationException("Failed to create budget.");
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

    private static string BuildDashboardQueryString(DateTime? from, DateTime? to, string? groupBy)
    {
        var parameters = new List<string>();
        if (from.HasValue) parameters.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) parameters.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        if (!string.IsNullOrWhiteSpace(groupBy)) parameters.Add($"groupBy={Uri.EscapeDataString(groupBy)}");
        return parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : string.Empty;
    }

    private static CostDashboard MapDashboard(CostDashboardWire? wire) => new()
    {
        TotalCost = wire?.TotalCost ?? 0,
        Trend = wire?.Trend?.Select(t => new CostTrendPoint { Date = t.Date, Cost = t.Cost }).ToList() ?? [],
        Breakdown = wire?.Breakdown?.Select(b => new CostBreakdownItem { Name = b.Name ?? string.Empty, Cost = b.Cost }).ToList() ?? []
    };

    private static CostRecommendation MapRecommendation(CostRecommendationWire wire) => new()
    {
        AgentId = wire.AgentId ?? string.Empty,
        AgentName = wire.AgentName ?? string.Empty,
        CurrentModel = wire.CurrentModel ?? string.Empty,
        RecommendedModel = wire.RecommendedModel ?? string.Empty,
        EstimatedSavings = wire.EstimatedSavings,
        Reason = wire.Reason ?? string.Empty,
        Priority = wire.Priority ?? string.Empty
    };

    private static CostBudget MapBudget(CostBudgetWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        Name = wire.Name ?? string.Empty,
        Amount = wire.Amount,
        Currency = wire.Currency ?? string.Empty,
        Period = wire.Period,
        Spent = wire.Spent,
        CreatedAt = wire.CreatedAt
    };
}

/// <summary>
/// Represents cost spend over a period, with a daily trend and a per-item breakdown.
/// </summary>
public class CostDashboard
{
    /// <summary>
    /// Gets or sets the total cost over the queried period.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the daily cost trend.
    /// </summary>
    public List<CostTrendPoint> Trend { get; set; } = [];

    /// <summary>
    /// Gets or sets the cost breakdown by item.
    /// </summary>
    public List<CostBreakdownItem> Breakdown { get; set; } = [];
}

/// <summary>
/// Represents the cost recorded for a single day.
/// </summary>
public class CostTrendPoint
{
    /// <summary>
    /// Gets or sets the date the cost was recorded.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the cost for the day.
    /// </summary>
    public decimal Cost { get; set; }
}

/// <summary>
/// Represents the cost attributed to a single item.
/// </summary>
public class CostBreakdownItem
{
    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cost attributed to the item.
    /// </summary>
    public decimal Cost { get; set; }
}

/// <summary>
/// Represents a cost optimization recommendation.
/// </summary>
public class CostRecommendation
{
    /// <summary>
    /// Gets or sets the identifier of the agent the recommendation applies to.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the agent the recommendation applies to.
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model the agent currently uses.
    /// </summary>
    public string CurrentModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model recommended to reduce cost.
    /// </summary>
    public string RecommendedModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated saving from applying the recommendation.
    /// </summary>
    public decimal EstimatedSavings { get; set; }

    /// <summary>
    /// Gets or sets the rationale for the recommendation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recommendation priority.
    /// </summary>
    public string Priority { get; set; } = string.Empty;
}

/// <summary>
/// Represents a cost budget.
/// </summary>
public class CostBudget
{
    /// <summary>
    /// Gets or sets the budget identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the budget name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the budget amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency of the budget amount.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the budget period.
    /// </summary>
    public string? Period { get; set; }

    /// <summary>
    /// Gets or sets the amount already spent against the budget.
    /// </summary>
    public decimal Spent { get; set; }

    /// <summary>
    /// Gets or sets the date the budget was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents a request to create a cost budget.
/// </summary>
public class CreateBudgetRequest
{
    /// <summary>
    /// Gets or sets the budget name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scope type the budget applies to.
    /// </summary>
    public string? ScopeType { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the scoped entity.
    /// </summary>
    public string? ScopeId { get; set; }

    /// <summary>
    /// Gets or sets the budget amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency of the budget amount.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the budget period.
    /// </summary>
    public string? Period { get; set; }

    /// <summary>
    /// Gets or sets the alert thresholds for the budget.
    /// </summary>
    public List<AlertThresholdRequest>? AlertThresholds { get; set; }

    /// <summary>
    /// Gets or sets the enforcement type of the budget.
    /// </summary>
    public string? EnforcementType { get; set; }
}

/// <summary>
/// Represents a budget alert threshold.
/// </summary>
public class AlertThresholdRequest
{
    /// <summary>
    /// Gets or sets the percentage of the budget at which the alert fires.
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Gets or sets the notification type used for the alert.
    /// </summary>
    public string? NotificationType { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CostDashboardWire
{
    public decimal TotalCost { get; set; }
    public List<CostTrendPointWire>? Trend { get; set; }
    public List<CostBreakdownItemWire>? Breakdown { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CostTrendPointWire
{
    public DateTime Date { get; set; }
    public decimal Cost { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CostBreakdownItemWire
{
    public string? Name { get; set; }
    public decimal Cost { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CostRecommendationWire
{
    public string? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? CurrentModel { get; set; }
    public string? RecommendedModel { get; set; }
    public decimal EstimatedSavings { get; set; }
    public string? Reason { get; set; }
    public string? Priority { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CostBudgetWire
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Period { get; set; }
    public decimal Spent { get; set; }
    public DateTime CreatedAt { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CreateBudgetResponse
{
    public string Id { get; set; } = string.Empty;
}
