using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Observability.Application.Commands;

internal sealed partial class IngestTraceCommandHandler(
    ITraceStore traceStore,
    IEventBus eventBus,
    ICacheService cacheService,
    IAgentRepository agentRepository,
    IDateTimeProvider dateTimeProvider,
    ILogger<IngestTraceCommandHandler> logger)
    : ICommandHandler<IngestTraceCommand, TraceId>
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent {AgentId} not found in cache during trace ingestion")]
    private static partial void LogAgentNotInCache(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Silent failure detected in trace {TraceId} for agent {AgentId}")]
    private static partial void LogSilentFailureDetected(ILogger logger, string traceId, string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to store trace {TraceId}")]
    private static partial void LogTraceStoreFailed(ILogger logger, Exception exception, string traceId);


    [LoggerMessage(Level = LogLevel.Debug, Message = "Trace ingested: {TraceId}")]
    private static partial void LogTraceIngested(ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent status cache unavailable during trace ingestion; proceeding without agent validation")]
    private static partial void LogCacheUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Prompt injection blocked during trace ingestion for agent {AgentId}: {OperationName}")]
    private static partial void LogPromptInjectionBlocked(ILogger logger, string agentId, string operationName);

    public async Task<Shared.Domain.Result<TraceId>> Handle(IngestTraceCommand request, CancellationToken ct)
    {
        // Validate agent exists and is active. Cache unavailability is treated as a
        // miss (same as an unknown agent) - ingestion must not depend on Redis.
        AgentStatusDto? agentCached = null;
        try
        {
            agentCached = await cacheService.GetAsync<AgentStatusDto>(
                $"seldonvigil:identity:{request.AgentId}", ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogCacheUnavailable(logger, ex);
        }

        if (agentCached is null)
        {
            // Cache miss: fall back to the agent repository so traces can only
            // be ingested for agents that actually exist.
            var agent = await agentRepository.GetByIdAsync(request.AgentId, ct).ConfigureAwait(false);
            if (agent is null)
            {
                LogAgentNotInCache(logger, request.AgentId.ToString());
                return Shared.Domain.Result<TraceId>.Failure(Error.NotFound(
                    "Trace.AgentNotFound",
                    $"Agent with ID '{request.AgentId}' was not found. Register the agent before ingesting traces."));
            }
        }
        else if (agentCached.Status != "Active")
        {
            return Shared.Domain.Result<TraceId>.Failure(Error.Forbidden(
                "Trace.AgentNotActive",
                $"Cannot ingest traces for agent in status '{agentCached.Status}'. Agent must be Active."));
        }

        // Synchronous prompt-injection screen on free-text trace fields.
        if (TraceThreatScreen.IsPromptInjection(request.OperationName, request.Attributes))
        {
            LogPromptInjectionBlocked(logger, request.AgentId.ToString(), request.OperationName ?? string.Empty);
            return Shared.Domain.Result<TraceId>.Failure(Error.Forbidden(
                "Trace.PromptInjectionDetected",
                "Trace payload rejected: potential prompt injection detected."));
        }

        var traceResult = Trace.Create(
            request.TenantId,
            request.AgentId,
            request.ParentTraceId,
            request.OperationName,
            request.OperationType,
            request.StartTime,
            request.EndTime,
            request.Status,
            request.Attributes,
            request.Events,
            request.Links,
            request.TokenUsage,
            request.Cost,
            request.IsAsync,
            request.Id,
            request.ModelName);

        if (traceResult.IsFailure)
        {
            return Shared.Domain.Result<TraceId>.Failure(traceResult.Error!);
        }

        var trace = traceResult.Value!;

        // Silent failure detection
        if (trace.HasSilentFailure())
        {
            await eventBus.PublishAsync(new SilentFailureDetectedEvent(
                trace.Id,
                trace.AgentId,
                trace.OperationName,
                dateTimeProvider.UtcNow), ct).ConfigureAwait(false);

            LogSilentFailureDetected(logger, trace.Id.ToString(), trace.AgentId.ToString());
        }

        // Persist before publishing so downstream workers never observe a trace
        // that is not yet stored.
        await traceStore.StoreAsync(trace, ct).ConfigureAwait(false);

        // Publish to the event broker for downstream processing
        // (canonical Domain.Events contract).
        // Carry operation context AND the caller's attributes on the event so that
        // downstream consumers (e.g. SecurityAnalyzerWorker rule evaluation) can see
        // the full trace payload without re-reading the database.
        var metadata = new Dictionary<string, object>
        {
            ["operationType"] = trace.OperationType.ToString(),
            ["operationName"] = trace.OperationName,
            ["status"] = trace.Status.ToString(),
            ["durationMs"] = trace.Duration.TotalMilliseconds
        };
        foreach (var attribute in trace.Attributes)
        {
            metadata.TryAdd(attribute.Key, attribute.Value);
        }

        await eventBus.PublishAsync(new TraceIngestedEvent(
            trace.Id,
            trace.AgentId,
            trace.TenantId,
            metadata,
            trace.Cost.Amount,
            trace.TokenUsage.TotalTokens)
        {
            Timestamp = dateTimeProvider.UtcNow
        }, ct).ConfigureAwait(false);

        LogTraceIngested(logger, trace.Id.ToString());

        return Result.Success(trace.Id);
    }
}
