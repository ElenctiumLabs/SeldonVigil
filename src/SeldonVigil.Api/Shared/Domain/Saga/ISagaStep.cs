namespace SeldonVigil.Shared.Domain.Saga;

/// <summary>
/// Represents a step in a saga that can be executed and compensated.
/// </summary>
public interface ISagaStep
{
    /// <summary>
    /// Gets the step name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the step order.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Executes the step asynchronously.
    /// </summary>
    /// <param name="sagaData">The saga data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    Task ExecuteAsync(object sagaData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates (rolls back) the step asynchronously.
    /// </summary>
    /// <param name="sagaData">The saga data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the compensation.</returns>
    Task CompensateAsync(object sagaData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for saga steps with typed saga data.
/// </summary>
/// <typeparam name="T">The type of saga data.</typeparam>
public abstract class SagaStep<T> : ISagaStep
{
    /// <summary>
    /// Gets the step name.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the step order.
    /// </summary>
    public abstract int Order { get; }

    /// <summary>
    /// Executes the step asynchronously.
    /// </summary>
    /// <param name="sagaData">The saga data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    public abstract Task ExecuteAsync(T sagaData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates (rolls back) the step asynchronously.
    /// </summary>
    /// <param name="sagaData">The saga data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the compensation.</returns>
    public abstract Task CompensateAsync(T sagaData, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    async Task ISagaStep.ExecuteAsync(object sagaData, CancellationToken cancellationToken)
    {
        if (sagaData is T typedData)
        {
            await ExecuteAsync(typedData, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidCastException($"Saga data must be of type {typeof(T).Name}");
        }
    }

    /// <inheritdoc/>
    async Task ISagaStep.CompensateAsync(object sagaData, CancellationToken cancellationToken)
    {
        if (sagaData is T typedData)
        {
            await CompensateAsync(typedData, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidCastException($"Saga data must be of type {typeof(T).Name}");
        }
    }
}
