using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.Governance;

/// <summary>
/// Client for governance policy and certification pipeline operations.
/// </summary>
public class GovernanceClient
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GovernanceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to communicate with the SeldonVigil API.</param>
    /// <param name="authClient">The authentication client used to obtain bearer tokens for API requests.</param>
    /// <param name="options">The SeldonVigil configuration options.</param>
    public GovernanceClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Lists all governance policies for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the list of governance policies for the current tenant.</returns>
    public async Task<List<GovernancePolicy>> ListPoliciesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/governance/policies/", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<GovernancePolicyWire>>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Select(MapPolicy).ToList() ?? [];
    }

    /// <summary>
    /// Gets a governance policy by ID.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the governance policy, or <see langword="null"/> when no policy with the specified ID exists.</returns>
    public async Task<GovernancePolicy?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(policyId, out var guid))
            throw new ArgumentException("Policy ID must be a valid GUID.", nameof(policyId));

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/governance/policies/{guid}", null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        var wire = await response.Content.ReadFromJsonAsync<GovernancePolicyWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? null : MapPolicy(wire);
    }

    /// <summary>
    /// Creates a new governance policy.
    /// </summary>
    /// <param name="request">The details of the policy to create.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the newly created governance policy.</returns>
    public async Task<GovernancePolicy> CreatePolicyAsync(CreateGovernancePolicyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/governance/policies/", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<GovernancePolicyWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to create policy.") : MapPolicy(wire);
    }

    /// <summary>
    /// Activates a governance policy.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy to activate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the activated governance policy.</returns>
    public async Task<GovernancePolicy> ActivatePolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(policyId, out var guid))
            throw new ArgumentException("Policy ID must be a valid GUID.", nameof(policyId));

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/governance/policies/{guid}/activate", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<GovernancePolicyWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to activate policy.") : MapPolicy(wire);
    }

    /// <summary>
    /// Triggers a certification pipeline for an agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to certify.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the triggered certification pipeline.</returns>
    public async Task<CertificationPipeline> TriggerCertificationAsync(string agentId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/governance/certification/trigger",
            new { agentId }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<CertificationPipelineWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to trigger certification.") : MapPipeline(wire);
    }

    /// <summary>
    /// Gets a certification pipeline by ID.
    /// </summary>
    /// <param name="pipelineId">The unique identifier of the certification pipeline to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the certification pipeline, or <see langword="null"/> when no pipeline with the specified ID exists.</returns>
    public async Task<CertificationPipeline?> GetCertificationAsync(string pipelineId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(pipelineId, out var guid))
            throw new ArgumentException("Pipeline ID must be a valid GUID.", nameof(pipelineId));

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/governance/certification/{guid}", null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        var wire = await response.Content.ReadFromJsonAsync<CertificationPipelineWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? null : MapPipeline(wire);
    }

    /// <summary>
    /// Approves a certification pipeline.
    /// </summary>
    /// <param name="pipelineId">The unique identifier of the certification pipeline to approve.</param>
    /// <param name="notes">Optional notes to attach to the approval.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the approved certification pipeline.</returns>
    public async Task<CertificationPipeline> ApproveCertificationAsync(string pipelineId, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(pipelineId, out var guid))
            throw new ArgumentException("Pipeline ID must be a valid GUID.", nameof(pipelineId));

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/governance/certification/{guid}/approve",
            new { notes }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<CertificationPipelineWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to approve certification.") : MapPipeline(wire);
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

    private static GovernancePolicy MapPolicy(GovernancePolicyWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        TenantId = wire.TenantId ?? string.Empty,
        Name = wire.Name ?? string.Empty,
        Description = wire.Description ?? string.Empty,
        Category = wire.Category ?? string.Empty,
        DefinitionYaml = wire.DefinitionYaml ?? string.Empty,
        Status = wire.Status ?? string.Empty,
        Version = wire.Version,
        PreviousVersionId = wire.PreviousVersionId,
        CreatedAt = wire.CreatedAt,
        ActivatedAt = wire.ActivatedAt,
        DeprecatedAt = wire.DeprecatedAt
    };

    private static CertificationPipeline MapPipeline(CertificationPipelineWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        TenantId = wire.TenantId ?? string.Empty,
        AgentId = wire.AgentId ?? string.Empty,
        Status = wire.Status ?? string.Empty,
        CurrentStage = wire.CurrentStage,
        Stages = wire.Stages?.Select(s => new PipelineStage
        {
            Name = s.Name ?? string.Empty,
            Description = s.Description ?? string.Empty,
            IsRequired = s.IsRequired,
            Status = s.Status ?? string.Empty,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            Output = s.Output
        }).ToList() ?? [],
        StartedAt = wire.StartedAt,
        CompletedAt = wire.CompletedAt,
        ApprovalNotes = wire.ApprovalNotes
    };
}

/// <summary>
/// Represents a governance policy belonging to a tenant.
/// </summary>
public class GovernancePolicy
{
    /// <summary>
    /// Gets or sets the unique identifier of the policy.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the identifier of the tenant that owns the policy.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name of the policy.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the policy.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the category the policy belongs to.
    /// </summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the YAML definition of the policy.
    /// </summary>
    public string DefinitionYaml { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the current status of the policy.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version number of the policy.
    /// </summary>
    public int Version { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the previous policy version, if any.
    /// </summary>
    public string? PreviousVersionId { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp when the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp when the policy was activated, if it has been activated.
    /// </summary>
    public DateTime? ActivatedAt { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp when the policy was deprecated, if it has been deprecated.
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }
}

/// <summary>
/// Represents the data required to create a governance policy.
/// </summary>
public class CreateGovernancePolicyRequest
{
    /// <summary>
    /// Gets or sets the display name of the policy to create.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the policy to create.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the category of the policy to create.
    /// </summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the YAML definition of the policy to create.
    /// </summary>
    public string DefinitionYaml { get; set; } = string.Empty;
}

/// <summary>
/// Represents a certification pipeline for an agent.
/// </summary>
public class CertificationPipeline
{
    /// <summary>
    /// Gets or sets the unique identifier of the certification pipeline.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the identifier of the tenant that owns the pipeline.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the identifier of the agent being certified.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the current status of the pipeline.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the current stage, if the pipeline has started.
    /// </summary>
    public string? CurrentStage { get; set; }
    /// <summary>
    /// Gets or sets the ordered list of stages in the pipeline.
    /// </summary>
    public List<PipelineStage> Stages { get; set; } = [];
    /// <summary>
    /// Gets or sets the UTC timestamp when the pipeline started.
    /// </summary>
    public DateTime StartedAt { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp when the pipeline completed, if it has completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    /// <summary>
    /// Gets or sets notes supplied when the pipeline was approved, if any.
    /// </summary>
    public string? ApprovalNotes { get; set; }
}

/// <summary>
/// Represents a single stage within a certification pipeline.
/// </summary>
public class PipelineStage
{
    /// <summary>
    /// Gets or sets the name of the stage.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the stage.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the stage is required for certification to pass.
    /// </summary>
    public bool IsRequired { get; set; }
    /// <summary>
    /// Gets or sets the current status of the stage.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the UTC timestamp when the stage started, if it has started.
    /// </summary>
    public DateTime? StartedAt { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp when the stage completed, if it has completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    /// <summary>
    /// Gets or sets the output produced by the stage, if any.
    /// </summary>
    public string? Output { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class GovernancePolicyWire
{
    public string? Id { get; set; }
    public string? TenantId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? DefinitionYaml { get; set; }
    public string? Status { get; set; }
    public int Version { get; set; }
    public string? PreviousVersionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeprecatedAt { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class CertificationPipelineWire
{
    public string? Id { get; set; }
    public string? TenantId { get; set; }
    public string? AgentId { get; set; }
    public string? Status { get; set; }
    public string? CurrentStage { get; set; }
    public List<PipelineStageWire>? Stages { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ApprovalNotes { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class PipelineStageWire
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public string? Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Output { get; set; }
}
