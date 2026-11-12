using Confluent.Kafka;
using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using SeldonVigil.Api.Modules.Observability.Infrastructure;
using SeldonVigil.SharedKernel.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SeldonVigil.Workers.Workers;

internal sealed partial class TraceProcessorWorker(
    [FromKeyedServices("trace-processor")]
    IConsumer<string, TraceIngestedEvent> consumer,
    ITraceEnrichmentService enrichmentService,
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus,
    IProducer<string, string> dlqProducer,
    ILogger<TraceProcessorWorker> logger)
    : BackgroundService
{
    private const int MaxRetries = 3;
    private const int RetryBackoffMs = 500;

    [LoggerMessage(Level = LogLevel.Information, Message = "Trace Processor Worker started")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processed trace {TraceId}")]
    private static partial void LogTraceProcessed(ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Broker consume error on trace topic")]
    private static partial void LogConsumeError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process trace {TraceId}")]
    private static partial void LogTraceProcessingFailed(ILogger logger, Exception exception, string traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transient failure processing trace {TraceId} (attempt {Attempt}/{MaxRetries})")]
    private static partial void LogTransientFailure(ILogger logger, Exception exception, string traceId, int attempt, int maxRetries);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Forwarded failed trace to {DlqTopic}")]
    private static partial void LogTraceForwardedToDlq(ILogger logger, string dlqTopic);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to forward trace to DLQ {DlqTopic}")]
    private static partial void LogTraceForwardToDlqFailed(ILogger logger, Exception exception, string dlqTopic);

    [RequiresDynamicCode("Calls SeldonVigil.Workers.Workers.TraceProcessorWorker.ForwardToDlqAsync(ConsumeResult<String, TraceIngestedEvent>, Exception, CancellationToken)")]
    [RequiresUnreferencedCode("Calls SeldonVigil.Workers.Workers.TraceProcessorWorker.ForwardToDlqAsync(ConsumeResult<String, TraceIngestedEvent>, Exception, CancellationToken)")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, TraceIngestedEvent>? message = null;
            try
            {
                message = consumer.Consume(stoppingToken);
                var trace = message.Message.Value;

                await ProcessWithRetryAsync(trace, stoppingToken).ConfigureAwait(false);

                consumer.Commit(message);
                LogTraceProcessed(logger, trace.TraceId.ToString());
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                LogConsumeError(logger, ex);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogTraceProcessingFailed(logger, ex,
                    message?.Message.Value?.TraceId.ToString() ?? "unknown");
                await ForwardToDlqAsync(message, ex, stoppingToken).ConfigureAwait(false);
                if (message is not null)
                {
                    consumer.Commit(message);
                }
            }
        }
    }

    private async Task ProcessWithRetryAsync(TraceIngestedEvent trace, CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                await ProcessOnceAsync(trace, ct).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                LogTransientFailure(logger, ex, trace.TraceId.ToString(), attempt, MaxRetries);
                await Task.Delay(TimeSpan.FromMilliseconds(RetryBackoffMs * attempt), ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessOnceAsync(TraceIngestedEvent trace, CancellationToken ct)
    {
        // Ambient tenant: global query filters in scoped DbContexts resolve the
        // message's tenant for the duration of this processing step.
        using var _ = TenantScope.Begin(trace.TenantId.Value);

        var enrichedTrace = await enrichmentService.EnrichAsync(trace, ct).ConfigureAwait(false);

        // ITraceEventStore is scoped (it owns an ObservabilityDbContext), so resolve
        // it per message instead of capturing it in this long-lived singleton.
        using (var scope = scopeFactory.CreateScope())
        {
            var traceStore = scope.ServiceProvider.GetRequiredService<ITraceEventStore>();
            await traceStore.StoreAsync(enrichedTrace, ct).ConfigureAwait(false);
        }

        await eventBus.PublishAsync(new TraceEnrichedEvent(
            trace.TraceId,
            trace.AgentId,
            enrichedTrace.Cost,
            enrichedTrace.TokenUsage)
        {
            Timestamp = DateTime.UtcNow
        }, ct).ConfigureAwait(false);
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task ForwardToDlqAsync(
        ConsumeResult<string, TraceIngestedEvent>? message,
        Exception failure,
        CancellationToken ct)
    {
        if (message is null)
        {
            return;
        }

        var dlqTopic = $"{nameof(TraceIngestedEvent)}.dlq";
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                OriginalTraceId = message.Message.Value?.TraceId,
                Error = failure.Message,
                FailedAtUtc = DateTime.UtcNow,
                RawValue = message.Message?.Value
            });

            _ = await dlqProducer.ProduceAsync(
                dlqTopic,
                new Message<string, string>
                {
                    Key = message.Message?.Key ?? throw new InvalidOperationException(),
                    Value = payload
                },
                ct).ConfigureAwait(false);

            LogTraceForwardedToDlq(logger, dlqTopic);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogTraceForwardToDlqFailed(logger, ex, dlqTopic);
        }
    }
}
