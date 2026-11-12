namespace SeldonVigil.Shared.Domain.Outbox;

/// <summary>
/// Represents an outbox message for reliable event publishing.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the outbox message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event type name.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event payload as JSON.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the processing status.
    /// </summary>
    public OutboxStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Represents the processing status of an outbox message.
/// </summary>
public enum OutboxStatus
{
    /// <summary>
    /// Message is pending processing.
    /// </summary>
    Pending,

    /// <summary>
    /// Message is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Message was processed successfully.
    /// </summary>
    Processed,

    /// <summary>
    /// Message processing failed.
    /// </summary>
    Failed
}
