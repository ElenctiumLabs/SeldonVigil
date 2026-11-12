using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.Security;

/// <summary>
/// Client for security policy operations in the SeldonVigil platform.
/// </summary>
public class SecurityClient
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="authClient">The authentication client.</param>
    /// <param name="options">The configuration options.</param>
    public SecurityClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Lists security policies with optional filtering.
    /// </summary>
    /// <param name="category">Optional category used to filter the returned policies.</param>
    /// <param name="status">Optional status used to filter the returned policies.</param>
    /// <param name="page">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of policies to return per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task whose result contains the requested page of policy summary items.</returns>
    public async Task<List<SecurityPolicyListItem>> ListPoliciesAsync(string? category = null, string? status = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(category)) query += $"&category={Uri.EscapeDataString(category)}";
        if (!string.IsNullOrWhiteSpace(status)) query += $"&status={Uri.EscapeDataString(status)}";

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/security/policies{query}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PolicyListResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Items?.Select(MapPolicyListItem).ToList() ?? [];
    }

    /// <summary>
    /// Gets a security policy by ID.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task whose result contains the policy details, or <see langword="null"/> when no policy with the specified ID exists.</returns>
    public async Task<SecurityPolicyDetail?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(policyId, out var guid))
            throw new ArgumentException("Policy ID must be a valid GUID.", nameof(policyId));

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/security/policies/{guid}", null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        var wire = await response.Content.ReadFromJsonAsync<SecurityPolicyDetailWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? null : MapPolicyDetail(wire);
    }

    /// <summary>
    /// Creates a new security policy.
    /// </summary>
    /// <param name="request">The policy definition to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task whose result contains the identifier of the newly created policy.</returns>
    public async Task<string> CreatePolicyAsync(CreateSecurityPolicyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/security/policies", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreatePolicyResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Id ?? throw new InvalidOperationException("Failed to create policy.");
    }

    /// <summary>
    /// Disables a security policy.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy to disable.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous policy disable operation.</returns>
    public async Task DisablePolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(policyId, out var guid))
            throw new ArgumentException("Policy ID must be a valid GUID.", nameof(policyId));

        using var response = await SendAsync(HttpMethod.Delete, $"api/{_options.ApiVersion}/security/policies/{guid}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Evaluates submitted input against the agent's active security policies
    /// (synchronous content scanning, e.g. prompt-injection detection).
    /// </summary>
    /// <param name="evaluation">The policy evaluation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The policy evaluation result.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when evaluation is null.</exception>
    public async Task<PolicyEvaluationResult> EvaluatePolicyAsync(PolicyEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluation);

        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/{_options.ApiVersion}/security/evaluate-input",
            evaluation,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PolicyEvaluationResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);

        return new PolicyEvaluationResult
        {
            IsAllowed = result?.IsAllowed ?? false,
            Violations = result?.Violations?
                .Select(v => new PolicyViolation
                {
                    PolicyId = v.PolicyId ?? string.Empty,
                    PolicyName = v.PolicyName ?? string.Empty,
                    Severity = Enum.TryParse<PolicySeverity>(v.Severity, ignoreCase: true, out var severity)
                        ? severity
                        : PolicySeverity.Info,
                    Message = v.Message ?? string.Empty
                })
                .ToList() ?? []
        };
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

    private static SecurityPolicyListItem MapPolicyListItem(PolicyListItemWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        Name = wire.Name ?? string.Empty,
        Description = wire.Description,
        Category = wire.Category,
        Severity = wire.Severity,
        IsEnabled = wire.IsEnabled,
        CreatedAt = wire.CreatedAt
    };

    private static SecurityPolicyDetail MapPolicyDetail(SecurityPolicyDetailWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        Name = wire.Name ?? string.Empty,
        Description = wire.Description,
        Category = wire.Category,
        Severity = wire.Severity,
        IsEnabled = wire.IsEnabled,
        AutoResponse = wire.AutoResponse,
        CreatedAt = wire.CreatedAt,
        UpdatedAt = wire.UpdatedAt
    };
}

/// <summary>
/// Request for policy evaluation.
/// </summary>
public class PolicyEvaluation
{
    /// <summary>
    /// Gets or sets the agent ID.
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input data.
    /// </summary>
    public Dictionary<string, object> Input { get; set; } = [];

    /// <summary>
    /// Gets or sets the output data.
    /// </summary>
    public Dictionary<string, object> Output { get; set; } = [];

    /// <summary>
    /// Gets or sets the context attributes.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = [];
}

/// <summary>
/// Result of policy evaluation.
/// </summary>
public class PolicyEvaluationResult
{
    /// <summary>
    /// Gets or sets whether the operation is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the violated policies.
    /// </summary>
    public List<PolicyViolation> Violations { get; set; } = [];

    /// <summary>
    /// Gets or sets the evaluation metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}

/// <summary>
/// Represents a policy violation.
/// </summary>
public class PolicyViolation
{
    /// <summary>
    /// Gets or sets the policy ID.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the violation severity.
    /// </summary>
    public PolicySeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the violation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action taken.
    /// </summary>
    public PolicyAction Action { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class PolicyEvaluationResponse
{
    public bool IsAllowed { get; set; }
    public List<PolicyViolationDto>? Violations { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class PolicyViolationDto
{
    public string? PolicyId { get; set; }
    public string? PolicyName { get; set; }
    public string? Severity { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Summary item returned by the policy list endpoint.
/// </summary>
public class SecurityPolicyListItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the policy.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name of the policy.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the policy, if any.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Gets or sets the category the policy belongs to, if any.
    /// </summary>
    public string? Category { get; set; }
    /// <summary>
    /// Gets or sets the severity level of the policy, if any.
    /// </summary>
    public string? Severity { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Full detail of a security policy.
/// </summary>
public class SecurityPolicyDetail
{
    /// <summary>
    /// Gets or sets the unique identifier of the policy.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name of the policy.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the policy, if any.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Gets or sets the category the policy belongs to, if any.
    /// </summary>
    public string? Category { get; set; }
    /// <summary>
    /// Gets or sets the severity level of the policy, if any.
    /// </summary>
    public string? Severity { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets the automatic response action applied when the policy is violated, if any.
    /// </summary>
    public string? AutoResponse { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the policy was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request body for creating a security policy.
/// </summary>
public class CreateSecurityPolicyRequest
{
    /// <summary>
    /// Gets or sets the display name of the policy to create.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the policy to create, if any.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Gets or sets the category the policy belongs to, if any.
    /// </summary>
    public string? Category { get; set; }
    /// <summary>
    /// Gets or sets the rule definition that specifies what the policy detects.
    /// </summary>
    public string RuleDefinition { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the severity level assigned to violations of the policy, if any.
    /// </summary>
    public string? Severity { get; set; }
    /// <summary>
    /// Gets or sets the automatic response action applied when the policy is violated, if any.
    /// </summary>
    public string? AutoResponse { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class PolicyListResponse
{
    public List<PolicyListItemWire>? Items { get; set; }
    public int TotalCount { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class PolicyListItemWire
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class SecurityPolicyDetailWire
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public bool IsEnabled { get; set; }
    public string? AutoResponse { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CreatePolicyResponse
{
    public string Id { get; set; } = string.Empty;
}
