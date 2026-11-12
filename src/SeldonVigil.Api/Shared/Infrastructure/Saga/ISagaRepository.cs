using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.Shared.Infrastructure.Saga;

/// <summary>
/// Interface for saga persistence.
/// </summary>
public interface ISagaRepository
{
    /// <summary>
    /// Saves a saga instance.
    /// </summary>
    /// <param name="sagaInstance">The saga instance to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the save operation.</returns>
    Task SaveAsync(SagaInstance sagaInstance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a saga instance by ID.
    /// </summary>
    /// <param name="sagaInstanceId">The saga instance ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saga instance or null if not found.</returns>
    Task<SagaInstance?> GetByIdAsync(Guid sagaInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saga instances by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of saga instances.</returns>
    Task<List<SagaInstance>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saga instances that are in a specific state.
    /// </summary>
    /// <param name="state">The saga state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of saga instances.</returns>
    Task<List<SagaInstance>> GetByStateAsync(SagaState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saga instances that need retry (failed with retry count below max).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of saga instances.</returns>
    Task<List<SagaInstance>> GetPendingRetryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a saga instance by ID.
    /// </summary>
    /// <param name="sagaInstanceId">The saga instance ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the delete operation.</returns>
    Task DeleteAsync(Guid sagaInstanceId, CancellationToken cancellationToken = default);
}
