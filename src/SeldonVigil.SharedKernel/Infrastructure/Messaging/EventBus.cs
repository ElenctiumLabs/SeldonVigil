using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeldonVigil.SharedKernel.Domain;
using System.Text.Json;

namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

public sealed partial class EventBus(
    IProducer<string, string> producer,
    IBrokerConsumerFactory consumerFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<BrokerConsumerOptions> options,
    ILogger<EventBus> logger)
    : IEventBus
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Handled {EventType} from {Topic} at offset {Offset}")]
    private static partial void LogHandledEvent(ILogger logger, string eventType, string topic, long offset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Consume error on topic {Topic}")]
    private static partial void LogConsumeError(ILogger logger, Exception exception, string topic);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error consuming topic {Topic}")]
    private static partial void LogUnexpectedConsumeError(ILogger logger, Exception exception, string topic);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Forwarded poison message from {Topic} to {DlqTopic}: {Error}")]
    private static partial void LogPoisonMessageForwarded(ILogger logger, string topic, string dlqTopic, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to forward poison message to {DlqTopic}")]
    private static partial void LogPoisonMessageForwardFailed(ILogger logger, Exception exception, string dlqTopic);

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
    {
        var topic = typeof(TEvent).Name;
        var key = Guid.NewGuid().ToString();

        try
        {
            var message = JsonSerializer.Serialize(@event);
            _ = await producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = key,
                    Value = message
                },
                ct).ConfigureAwait(false);
        }
        catch (ProduceException<string, string> ex)
        {
            throw new InvalidOperationException($"Failed to publish event of type {typeof(TEvent).Name}", ex);
        }
    }

    public Task SubscribeAsync<TEvent, THandler>(CancellationToken ct = default)
        where TEvent : BaseEvent
        where THandler : IEventHandler<TEvent>
    {
        var topic = typeof(TEvent).Name;
        var consumerGroup = options.Value.ConsumerGroup;
        var consumer = consumerFactory.CreateConsumer<string>(
            topic,
            consumerGroup,
            Deserializers.Utf8);

        _ = Task.Run(() => ConsumeLoopAsync<TEvent, THandler>(consumer, topic, ct), CancellationToken.None);

        return Task.CompletedTask;
    }

    private async Task ConsumeLoopAsync<TEvent, THandler>(
        IConsumer<string, string> consumer,
        string topic,
        CancellationToken ct)
        where TEvent : BaseEvent
        where THandler : IEventHandler<TEvent>
    {
        var dlqTopic = topic + options.Value.DlqSuffix;
        try
        {
            consumer.Subscribe(topic);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(ct);
                    if (result is null || result.Message is null)
                    {
                        continue;
                    }

                    TEvent? @event;
                    try
                    {
                        @event = JsonSerializer.Deserialize<TEvent>(result.Message.Value);
                    }
                    catch (JsonException ex)
                    {
                        await ForwardToDlqAsync(dlqTopic, topic, result, ex, ct).ConfigureAwait(false);
                        consumer.Commit(result);
                        continue;
                    }

                    if (@event is null)
                    {
                        await ForwardToDlqAsync(
                            dlqTopic,
                            topic,
                            result,
                            new InvalidOperationException("Deserialized event is null"),
                            ct).ConfigureAwait(false);
                        consumer.Commit(result);
                        continue;
                    }

                    using var scope = scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                    await handler.HandleAsync(@event, ct).ConfigureAwait(false);

                    consumer.Commit(result);
                    LogHandledEvent(logger, typeof(TEvent).Name, topic, result.Offset.Value);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    LogConsumeError(logger, ex, topic);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogUnexpectedConsumeError(logger, ex, topic);
                }
            }
        }
        finally
        {
            consumer.Close();
            consumer.Dispose();
        }
    }

    private async Task ForwardToDlqAsync(
        string dlqTopic,
        string originalTopic,
        ConsumeResult<string, string> result,
        Exception failure,
        CancellationToken ct)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                OriginalTopic = originalTopic,
                Partition = result.Partition.Value,
                Offset = result.Offset.Value,
                result.Message?.Key,
                result.Message?.Value,
                Error = failure.Message,
                FailedAtUtc = DateTime.UtcNow
            });

            _ = await producer.ProduceAsync(
                dlqTopic,
                new Message<string, string>
                {
                    Key = result.Message?.Key ?? throw new InvalidOperationException(),
                    Value = payload
                },
                ct).ConfigureAwait(false);

            LogPoisonMessageForwarded(logger, originalTopic, dlqTopic, failure.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogPoisonMessageForwardFailed(logger, ex, dlqTopic);
        }
    }
}
