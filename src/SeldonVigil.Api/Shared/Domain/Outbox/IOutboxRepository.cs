namespace SeldonVigil.Shared.Domain.Outbox;

/// <summary>
/// Interface for outbox message persistence.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Saves an outbox message.
    /// </summary>
    /// <param name="message">The outbox message to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the save operation.</returns>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending outbox messages.
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of pending outbox messages.</returns>
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the update operation.</returns>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the update operation.</returns>
    Task MarkAsFailedAsync(Guid messageId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages that need retry (failed with retry count below max).
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of messages pending retry.</returns>
    Task<List<OutboxMessage>> GetPendingRetryAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes processed messages older than the specified date.
    /// </summary>
    /// <param name="beforeDate">The cutoff date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of messages deleted.</returns>
    Task<int> DeleteProcessedAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
}
