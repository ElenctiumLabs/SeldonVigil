namespace SeldonVigil.Sdk.Security;

/// <summary>
/// Represents a security policy in the SeldonVigil platform.
/// </summary>
public class SecurityPolicy
{
    /// <summary>
    /// Gets or sets the policy ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the policy type.
    /// </summary>
    public PolicyType Type { get; set; }

    /// <summary>
    /// Gets or sets the policy severity.
    /// </summary>
    public PolicySeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the policy rules.
    /// </summary>
    public List<PolicyRule> Rules { get; set; } = new();

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
/// Represents the type of security policy.
/// </summary>
public enum PolicyType
{
    /// <summary>
    /// Data access policy.
    /// </summary>
    DataAccess,

    /// <summary>
    /// Content moderation policy.
    /// </summary>
    ContentModeration,

    /// <summary>
    /// Rate limiting policy.
    /// </summary>
    RateLimiting,

    /// <summary>
    /// Input validation policy.
    /// </summary>
    InputValidation,

    /// <summary>
    /// Output validation policy.
    /// </summary>
    OutputValidation,

    /// <summary>
    /// Custom policy.
    /// </summary>
    Custom
}

/// <summary>
/// Represents the severity of a security policy.
/// </summary>
public enum PolicySeverity
{
    /// <summary>
    /// Informational severity.
    /// </summary>
    Info,

    /// <summary>
    /// Low severity.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity.
    /// </summary>
    Critical
}

/// <summary>
/// Represents a rule within a security policy.
/// </summary>
public class PolicyRule
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule definition.
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule action.
    /// </summary>
    public PolicyAction Action { get; set; }
}

/// <summary>
/// Represents the action to take when a policy rule is violated.
/// </summary>
public enum PolicyAction
{
    /// <summary>
    /// Log the violation.
    /// </summary>
    Log,

    /// <summary>
    /// Warn the user.
    /// </summary>
    Warn,

    /// <summary>
    /// Block the operation.
    /// </summary>
    Block,

    /// <summary>
    /// Quarantine the data.
    /// </summary>
    Quarantine
}
