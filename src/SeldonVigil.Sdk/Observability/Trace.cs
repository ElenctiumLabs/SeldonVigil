namespace SeldonVigil.Sdk.Observability;

/// <summary>
/// Represents a trace in the SeldonVigil platform.
/// </summary>
public class SeldonVigilTrace
{
    /// <summary>
    /// Gets or sets the trace ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent ID.
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Gets or sets the agent name.
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Gets or sets the parent trace ID.
    /// </summary>
    public string? ParentTraceId { get; set; }

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Gets or sets the operation name.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trace status.
    /// </summary>
    public TraceStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the end timestamp.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the events.
    /// </summary>
    public List<TraceEvent> Events { get; set; } = new();

    /// <summary>
    /// Gets or sets the token usage.
    /// </summary>
    public TokenUsage? TokenUsage { get; set; }

    /// <summary>
    /// Gets or sets the cost.
    /// </summary>
    public decimal? Cost { get; set; }

    /// <summary>
    /// Gets or sets child traces or spans.
    /// </summary>
    public List<SeldonVigilTrace> Children { get; set; } = new();
}

/// <summary>
/// Represents the status of a trace.
/// </summary>
public enum TraceStatus
{
    /// <summary>
    /// The trace is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// The trace completed successfully.
    /// </summary>
    Ok,

    /// <summary>
    /// The trace completed with an error.
    /// </summary>
    Error,

    /// <summary>
    /// The trace completed in a degraded state.
    /// </summary>
    Degraded,

    /// <summary>
    /// The trace was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents an event in a trace.
/// </summary>
public class TraceEvent
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the event attributes.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Represents token usage for a trace.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Gets or sets the number of input (prompt) tokens.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output (completion) tokens.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of cached tokens.
    /// </summary>
    public int CachedTokens { get; set; }

    /// <summary>
    /// Gets the total number of tokens.
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
}
