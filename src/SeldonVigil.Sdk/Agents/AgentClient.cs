using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.Agents;

/// <summary>
/// Client for managing AI agents in the SeldonVigil platform.
/// </summary>
public class AgentClient
{
    // The API binds Framework/ModelProvider to enums; unknown names must be
    // normalized onto "Custom" instead of failing request validation.
    private static readonly HashSet<string> KnownFrameworks = new(StringComparer.OrdinalIgnoreCase)
        { "LangChain", "LangGraph", "CrewAI", "AutoGen", "SemanticKernel", "Custom" };

    private static readonly HashSet<string> KnownModelProviders = new(StringComparer.OrdinalIgnoreCase)
        { "OpenAI", "Anthropic", "Google", "Azure", "AwsBedrock", "Ollama", "multi-provider", "Custom" };

    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="authClient">The authentication client.</param>
    /// <param name="options">The configuration options.</param>
    public AgentClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Gets all agents for the current tenant.
    /// </summary>
    /// <param name="status">Optional filter by status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of agents.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    public async Task<List<Agent>> GetAgentsAsync(AgentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = status.HasValue ? $"?status={status}" : string.Empty;
        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/agents{query}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AgentListResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Items.Select(MapListItem).ToList() ?? [];
    }

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The agent, or null if not found.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when agent ID is null or empty.</exception>
    public async Task<Agent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(agentId, out var guid))
        {
            throw new ArgumentException("Agent ID must be a valid GUID.", nameof(agentId));
        }

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/agents/{guid}", null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var wire = await response.Content.ReadFromJsonAsync<AgentDetailWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? null : MapDetail(wire);
    }

    /// <summary>
    /// Registers a new agent.
    /// </summary>
    /// <param name="agent">The agent to register.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registered agent.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when agent is null.</exception>
    public async Task<Agent> RegisterAgentAsync(CreateAgentRequest agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/{_options.ApiVersion}/agents",
            new
            {
                externalId = agent.ExternalId,
                name = agent.Name,
                description = agent.Description,
                framework = NormalizeFramework(agent.Framework),
                frameworkVersion = agent.FrameworkVersion,
                modelProvider = NormalizeModelProvider(agent.ModelProvider),
                modelName = agent.ModelName,
                modelVersion = agent.ModelVersion,
                manifest = MapManifest(agent.Manifest),
            },
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // The API returns { id } on registration.
        var result = await response.Content.ReadFromJsonAsync<RegisterAgentResponse>(SdkJson.Options, cancellationToken).ConfigureAwait(false);

        return new Agent
        {
            Id = result?.Id ?? throw new InvalidOperationException("Failed to register agent."),
            ExternalId = agent.ExternalId,
            Name = agent.Name,
            Description = agent.Description,
            Framework = agent.Framework,
            ModelProvider = agent.ModelProvider,
            ModelName = agent.ModelName,
            CapabilityManifest = agent.Manifest,
            Status = AgentStatus.Draft
        };
    }

    /// <summary>
    /// Activates an agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="reason">The activation reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activated agent.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when agent ID is null or empty.</exception>
    public async Task<Agent> ActivateAgentAsync(string agentId, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(agentId, out var guid))
        {
            throw new ArgumentException("Agent ID must be a valid GUID.", nameof(agentId));
        }

        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/{_options.ApiVersion}/agents/{guid}/activate",
            reason is null ? null : new { reason },
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var agent = await GetAgentAsync(agentId, cancellationToken).ConfigureAwait(false);
        return agent ?? throw new InvalidOperationException("Failed to activate agent.");
    }

    /// <summary>
    /// Suspends an agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="reason">The suspension reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The suspended agent.</returns>
    public async Task<Agent> SuspendAgentAsync(string agentId, string reason, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(agentId, out var guid))
        {
            throw new ArgumentException("Agent ID must be a valid GUID.", nameof(agentId));
        }

        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/{_options.ApiVersion}/agents/{guid}/suspend",
            new { reason },
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var agent = await GetAgentAsync(agentId, cancellationToken).ConfigureAwait(false);
        return agent ?? throw new InvalidOperationException("Failed to suspend agent.");
    }

    /// <summary>
    /// Permanently revokes an agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="reason">The revocation reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The revoked agent.</returns>
    public async Task<Agent> RevokeAgentAsync(string agentId, string reason, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(agentId, out var guid))
        {
            throw new ArgumentException("Agent ID must be a valid GUID.", nameof(agentId));
        }

        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/{_options.ApiVersion}/agents/{guid}/revoke",
            new { reason },
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return new Agent
        {
            Id = guid.ToString(),
            Status = AgentStatus.Revoked
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

    private static Agent MapListItem(AgentListItemWire item) => new()
    {
        Id = item.Id,
        ExternalId = item.ExternalId ?? string.Empty,
        Name = item.Name,
        Framework = item.Framework ?? string.Empty,
        ModelProvider = item.ModelProvider ?? string.Empty,
        ModelName = item.ModelName ?? string.Empty,
        Status = ParseStatus(item.Status),
        CreatedAt = item.CreatedAt,
    };

    private static Agent MapDetail(AgentDetailWire detail) => new()
    {
        Id = detail.Id,
        TenantId = detail.TenantId ?? string.Empty,
        ExternalId = detail.ExternalId ?? string.Empty,
        Name = detail.Name,
        Description = detail.Description,
        Framework = detail.Framework ?? string.Empty,
        ModelProvider = detail.ModelProvider ?? string.Empty,
        ModelName = detail.ModelName ?? string.Empty,
        Status = ParseStatus(detail.Status),
        CreatedAt = detail.CreatedAt,
        UpdatedAt = detail.UpdatedAt,
    };

    private static AgentStatus ParseStatus(string? status) =>
        Enum.TryParse<AgentStatus>(status, ignoreCase: true, out var parsed) ? parsed : AgentStatus.Draft;

    internal static string NormalizeFramework(string framework) =>
        KnownFrameworks.Contains(framework) ? KnownFrameworks.First(f => f.Equals(framework, StringComparison.OrdinalIgnoreCase)) : "Custom";

    internal static string NormalizeModelProvider(string provider) =>
        KnownModelProviders.Contains(provider) ? KnownModelProviders.First(f => f.Equals(provider, StringComparison.OrdinalIgnoreCase)) : "Custom";

    /// <summary>
    /// Maps the SDK's simplified manifest onto the structured shape the API binds:
    /// tools/dataAccess entries are objects, not plain strings.
    /// </summary>
    internal static object? MapManifest(CapabilityManifest? manifest)
    {
        if (manifest is null)
        {
            return null;
        }

        return new
        {
            tools = manifest.Tools.Select(t => new { name = t, description = string.Empty, parameters = new { }, required = false }).ToList(),
            dataAccess = manifest.DataAccess.Select(d => new { resource = d, accessLevel = "read", allowedFields = Array.Empty<string>() }).ToList(),
            permissions = manifest.Permissions,
            rateLimits = manifest.RateLimits,
        };
    }
}

/// <summary>
/// Request to create a new agent.
/// </summary>
public class CreateAgentRequest
{
    /// <summary>
    /// Gets or sets the external ID.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the agent framework (free-form; unknown values register as Custom).
    /// </summary>
    public string Framework { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the framework version.
    /// </summary>
    public string? FrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets the model provider identifier (unknown values register as Custom).
    /// </summary>
    public string ModelProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public string? ModelVersion { get; set; }

    /// <summary>
    /// Gets or sets the capability manifest.
    /// </summary>
    public CapabilityManifest? Manifest { get; set; }
}

/// <summary>
/// Response containing a paginated list of agents.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class AgentListResponse
{
    public List<AgentListItemWire> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class AgentListItemWire
{
    public string Id { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Framework { get; set; }
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class AgentDetailWire
{
    public string Id { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Framework { get; set; }
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response from agent registration: { id }.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class RegisterAgentResponse
{
    public string Id { get; set; } = string.Empty;
}
