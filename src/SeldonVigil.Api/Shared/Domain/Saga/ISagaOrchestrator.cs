namespace SeldonVigil.Shared.Domain.Saga;

/// <summary>
/// Interface for saga orchestration.
/// </summary>
public interface ISagaOrchestrator
{
    /// <summary>
    /// Executes a saga with the given steps and data.
    /// </summary>
    /// <typeparam name="T">The type of saga data.</typeparam>
    /// <param name="sagaType">The saga type identifier.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="steps">The saga steps to execute.</param>
    /// <param name="sagaData">The saga data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the saga execution.</returns>
    Task ExecuteAsync<T>(
        string sagaType,
        string correlationId,
        IEnumerable<ISagaStep> steps,
        T sagaData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates a saga by executing compensation actions in reverse order.
    /// </summary>
    /// <param name="sagaInstanceId">The saga instance ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the compensation.</returns>
    Task CompensateAsync(
        Guid sagaInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a saga instance by ID.
    /// </summary>
    /// <param name="sagaInstanceId">The saga instance ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saga instance.</returns>
    Task<SagaInstance?> GetSagaInstanceAsync(
        Guid sagaInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saga instances by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saga instances.</returns>
    Task<List<SagaInstance>> GetSagaInstancesByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);
}
