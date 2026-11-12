namespace SeldonVigil.Shared.Domain.Saga;

/// <summary>
/// Represents the state of a saga instance for distributed transaction coordination.
/// </summary>
public class SagaInstance
{
    /// <summary>
    /// Gets or sets the unique identifier for the saga instance.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the saga type identifier.
    /// </summary>
    public string SagaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID for the business operation.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current state of the saga.
    /// </summary>
    public SagaState State { get; set; }

    /// <summary>
    /// Gets or sets the current step in the saga execution.
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets the total number of steps in the saga.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the saga data as JSON.
    /// </summary>
    public string SagaData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if the saga failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the saga completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

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
/// Represents the state of a saga instance.
/// </summary>
public enum SagaState
{
    /// <summary>
    /// Saga is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Saga is currently executing.
    /// </summary>
    InProgress,

    /// <summary>
    /// Saga completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Saga failed and requires compensation.
    /// </summary>
    Failed,

    /// <summary>
    /// Saga is compensating (rolling back).
    /// </summary>
    Compensating,

    /// <summary>
    /// Saga compensation completed.
    /// </summary>
    Compensated
}
