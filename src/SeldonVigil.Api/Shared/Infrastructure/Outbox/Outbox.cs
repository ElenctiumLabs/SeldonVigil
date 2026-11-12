using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.Shared.Domain.Outbox;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Interface for storing and retrieving outbox messages.
/// </summary>
public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

/// <summary>
/// PostgreSQL-backed outbox repository.
/// </summary>
internal class PostgresOutboxRepository(SeldonVigilDbContext dbContext) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await dbContext.OutboxMessages
            .Where(m => m.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.ProcessedAt, DateTime.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        await dbContext.OutboxMessages
            .Where(m => m.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.RetryCount, m => m.RetryCount + 1)
                .SetProperty(m => m.ErrorMessage, error),
                cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Outbox publisher that stores domain events in the database and publishes
/// them via the event bus. Use within a transaction to guarantee at-least-once delivery.
/// </summary>
public class OutboxPublisher(IOutboxRepository outboxRepository, ILogger<OutboxPublisher> logger)
{
    /// <summary>
    /// Stores a domain event in the outbox for reliable publishing.
    /// Call this within the same transaction as the aggregate modification.
    /// </summary>
    public async Task EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).FullName ?? typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            CreatedAt = DateTime.UtcNow,
        };

        await outboxRepository.AddAsync(message, cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Enqueued outbox message {MessageId} for event {EventType}", message.Id, message.EventType);
    }
}

/// <summary>
/// Background service that processes the outbox: polls for unprocessed messages,
/// publishes them to the event bus, and marks them as processed.
/// </summary>
public class OutboxProcessorService(
    IServiceProvider serviceProvider,
    IEventBus eventBus,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during outbox processing");
            }

            await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("OutboxProcessorService stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var messages = await outboxRepository.GetUnprocessedAsync(BatchSize, cancellationToken).ConfigureAwait(false);

        foreach (var message in messages)
        {
            try
            {
                // Reconstruct the event from the payload and publish via the event bus.
                // The event type is stored as the full type name; we deserialize into
                // the appropriate type and invoke PublishAsync via reflection.
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    logger.LogWarning("Unknown event type {EventType} for outbox message {MessageId}", message.EventType, message.Id);
                    await outboxRepository.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType);
                if (@event is null)
                {
                    logger.LogWarning("Failed to deserialize outbox message {MessageId}", message.Id);
                    await outboxRepository.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Use reflection to call PublishAsync<TEvent> on the event bus
                var publishMethod = typeof(IEventBus)
                    .GetMethod(nameof(IEventBus.PublishAsync))!
                    .MakeGenericMethod(eventType);

                await (Task)publishMethod.Invoke(eventBus, [@event, cancellationToken])!;

                await outboxRepository.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

                logger.LogDebug("Published outbox message {MessageId} ({EventType})", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                await outboxRepository.MarkFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
