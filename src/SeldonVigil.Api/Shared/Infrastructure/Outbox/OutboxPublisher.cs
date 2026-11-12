using Confluent.Kafka;
using SeldonVigil.Shared.Domain.Outbox;

namespace SeldonVigil.Shared.Infrastructure.Outbox;

/// <summary>
/// Background service for processing outbox messages and publishing to the broker.
/// </summary>
public sealed partial class OutboxPublisher(
    IOutboxRepository outboxRepository,
    IProducer<Null, string> brokerProducer,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxPublisher started")]
    private static partial void LogPublisherStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing outbox messages")]
    private static partial void LogProcessingError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxPublisher stopped")]
    private static partial void LogPublisherStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Publishing outbox message {MessageId} of type {EventType}")]
    private static partial void LogPublishingMessage(ILogger logger, Guid messageId, string eventType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully published outbox message {MessageId}")]
    private static partial void LogMessagePublished(ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to publish outbox message {MessageId}")]
    private static partial void LogPublishFailed(ILogger logger, Exception ex, Guid messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrying outbox message {MessageId} (attempt {RetryCount}/{MaxRetries})")]
    private static partial void LogRetryingMessage(ILogger logger, Guid messageId, int retryCount, int maxRetries);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully retried outbox message {MessageId}")]
    private static partial void LogMessageRetried(ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to retry outbox message {MessageId}")]
    private static partial void LogRetryFailed(ILogger logger, Exception ex, Guid messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaned up {Count} processed outbox messages older than {CutoffDate}")]
    private static partial void LogCleanup(ILogger logger, int count, DateTime cutoffDate);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogPublisherStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken).ConfigureAwait(false);
                await ProcessRetryMessagesAsync(stoppingToken).ConfigureAwait(false);
                await CleanupProcessedMessagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogProcessingError(logger, ex);
            }

            await Task.Delay(_processingInterval, stoppingToken).ConfigureAwait(false);
        }

        LogPublisherStopped(logger);
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await outboxRepository.GetPendingAsync(batchSize: 50, cancellationToken).ConfigureAwait(false);

        foreach (var message in messages)
        {
            try
            {
                message.Status = OutboxStatus.Processing;
                await outboxRepository.SaveAsync(message, cancellationToken).ConfigureAwait(false);

                LogPublishingMessage(logger, message.Id, message.EventType);

                var brokerMessage = new Message<Null, string>
                {
                    Value = message.Payload,
                    Headers = new Headers()
                };

                if (!string.IsNullOrEmpty(message.CorrelationId))
                {
                    brokerMessage.Headers.Add("correlation-id", System.Text.Encoding.UTF8.GetBytes(message.CorrelationId));
                }

                await brokerProducer.ProduceAsync(
                    message.EventType.ToLowerInvariant(),
                    brokerMessage,
                    cancellationToken).ConfigureAwait(false);

                await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

                LogMessagePublished(logger, message.Id);
            }
            catch (Exception ex)
            {
                LogPublishFailed(logger, ex, message.Id);
                await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessRetryMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await outboxRepository.GetPendingRetryAsync(batchSize: 20, cancellationToken).ConfigureAwait(false);

        foreach (var message in messages)
        {
            try
            {
                LogRetryingMessage(logger, message.Id, message.RetryCount + 1, message.MaxRetries);

                var brokerMessage = new Message<Null, string>
                {
                    Value = message.Payload,
                    Headers = new Headers()
                };

                if (!string.IsNullOrEmpty(message.CorrelationId))
                {
                    brokerMessage.Headers.Add("correlation-id", System.Text.Encoding.UTF8.GetBytes(message.CorrelationId));
                }

                await brokerProducer.ProduceAsync(
                    message.EventType.ToLowerInvariant(),
                    brokerMessage,
                    cancellationToken).ConfigureAwait(false);

                await outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

                LogMessageRetried(logger, message.Id);
            }
            catch (Exception ex)
            {
                LogRetryFailed(logger, ex, message.Id);
                await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task CleanupProcessedMessagesAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        var deletedCount = await outboxRepository.DeleteProcessedAsync(cutoffDate, cancellationToken).ConfigureAwait(false);

        if (deletedCount > 0)
        {
            LogCleanup(logger, deletedCount, cutoffDate);
        }
    }
}
