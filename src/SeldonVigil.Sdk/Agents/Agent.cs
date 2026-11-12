namespace SeldonVigil.Sdk.Agents;

/// <summary>
/// Represents an AI agent in the SeldonVigil platform.
/// </summary>
public class Agent
{
    /// <summary>
    /// Gets or sets the agent ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

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
    /// Gets or sets the agent status.
    /// </summary>
    public AgentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the agent framework.
    /// </summary>
    public string Framework { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model provider.
    /// </summary>
    public string ModelProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capability manifest.
    /// </summary>
    public CapabilityManifest? CapabilityManifest { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Represents the status of an agent.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// The agent is in draft state.
    /// </summary>
    Draft,

    /// <summary>
    /// The agent is pending certification.
    /// </summary>
    PendingCertification,

    /// <summary>
    /// The agent is active.
    /// </summary>
    Active,

    /// <summary>
    /// The agent is suspended.
    /// </summary>
    Suspended,

    /// <summary>
    /// The agent is revoked.
    /// </summary>
    Revoked,

    /// <summary>
    /// The agent is archived.
    /// </summary>
    Archived
}

/// <summary>
/// Represents the capability manifest of an agent.
/// </summary>
public class CapabilityManifest
{
    /// <summary>
    /// Gets or sets the tools available to the agent.
    /// </summary>
    public List<string> Tools { get; set; } = new();

    /// <summary>
    /// Gets or sets the data access rules.
    /// </summary>
    public List<string> DataAccess { get; set; } = new();

    /// <summary>
    /// Gets or sets the permissions.
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the rate limits.
    /// </summary>
    public Dictionary<string, int> RateLimits { get; set; } = new();
}
