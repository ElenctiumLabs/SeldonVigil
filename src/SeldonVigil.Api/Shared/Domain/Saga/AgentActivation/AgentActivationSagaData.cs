namespace SeldonVigil.Shared.Domain.Saga.AgentActivation;

/// <summary>
/// Data for the agent activation saga.
/// </summary>
public class AgentActivationSagaData
{
    /// <summary>
    /// Gets or sets the agent ID.
    /// </summary>
    public Guid AgentId { get; set; }

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
    /// Gets or sets the framework.
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
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the activation.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the reason for activation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the agent was registered in Identity module.
    /// </summary>
    public bool AgentRegistered { get; set; }

    /// <summary>
    /// Gets or sets whether security policies were applied.
    /// </summary>
    public bool SecurityPoliciesApplied { get; set; }
}
