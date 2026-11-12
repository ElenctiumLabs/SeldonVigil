using System.Diagnostics;
using Confluent.Kafka;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Events;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Workers.Workers;

internal sealed partial class SecurityAnalyzerWorker(
    [FromKeyedServices("security-analyzer")]
    IConsumer<string, TraceIngestedEvent> consumer,
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider,
    ILogger<SecurityAnalyzerWorker> logger)
    : BackgroundService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Security Analyzer Worker started")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Policy violation detected: {PolicyName} for agent {AgentId} on trace {TraceId}")]
    private static partial void LogPolicyViolation(ILogger logger, string policyName, AgentId agentId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Duplicate violation suppressed for policy {PolicyId} on trace {TraceId}")]
    private static partial void LogDuplicateSuppressed(ILogger logger, Guid policyId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Security analysis failed for trace {TraceId}")]
    private static partial void LogAnalysisFailed(ILogger logger, Exception exception, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Auto-suspend requested for agent {AgentId}")]
    private static partial void LogAutoSuspendRequested(ILogger logger, AgentId agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Auto-block configured for agent {AgentId}")]
    private static partial void LogAutoBlockConfigured(ILogger logger, AgentId agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Alert-only response for agent {AgentId}")]
    private static partial void LogAlertOnlyResponse(ILogger logger, AgentId agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Auto-quarantine requested for agent {AgentId}")]
    private static partial void LogAutoQuarantineRequested(ILogger logger, AgentId agentId);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, TraceIngestedEvent>? message = null;
            try
            {
                message = consumer.Consume(stoppingToken);

                var traceEvent = message.Message.Value ?? throw new InvalidOperationException("Received null trace payload.");

                await AnalyzeAsync(traceEvent, stoppingToken).ConfigureAwait(false);

                consumer.Commit(message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogAnalysisFailed(logger, ex, message?.Message.Value?.TraceId ?? TraceId.Empty);
            }
        }
    }

    private async Task AnalyzeAsync(TraceIngestedEvent traceEvent, CancellationToken ct)
    {
        using var activity = SecurityAnalyzerActivity.StartActivity(traceEvent.TraceId.ToString());

        // Ambient tenant: policy/violation queries are globally filtered.
        using var _ = TenantScope.Begin(traceEvent.TenantId.Value);

        var trace = ToDomainTrace(traceEvent);
        using var scope = scopeFactory.CreateScope();
        var policyRepository = scope.ServiceProvider.GetRequiredService<ISecurityPolicyRepository>();
        var violationRepository = scope.ServiceProvider.GetRequiredService<ISecurityViolationRepository>();

        // Get active policies for tenant
        var policies = await policyRepository.GetActiveByTenantAsync(
            trace.TenantId, ct).ConfigureAwait(false);

        foreach (var policy in policies)
        {
            ct.ThrowIfCancellationRequested();

            if (!policy.RuleDefinition.Evaluate(trace))
            {
                continue;
            }

            // Deduplicate: one violation per trace/policy pair, ever.
            if (await violationRepository.ExistsForTraceAsync(policy.Id, trace.Id, ct).ConfigureAwait(false))
            {
                LogDuplicateSuppressed(logger, policy.Id.Value, trace.Id);
                continue;
            }

            LogPolicyViolation(logger, policy.Name, trace.AgentId, trace.Id);

            var violation = SecurityViolation.Create(
                policy.Id,
                trace.TenantId,
                trace.AgentId,
                trace.Id,
                policy.Severity,
                $"Policy '{policy.Name}' violated by agent {trace.AgentId}",
                policy.RuleDefinition,
                dateTimeProvider);

            await violationRepository.AddAsync(violation, ct).ConfigureAwait(false);

            activity?.SetTag("security.violation_id", violation.Id.Value.ToString());
            activity?.SetTag("security.policy_id", policy.Id.Value.ToString());

            await eventBus.PublishAsync(new PolicyViolatedEvent(
                policy.Id,
                trace.TenantId,
                trace.AgentId,
                trace.Id,
                policy.Severity,
                policy.AutoResponse,
                dateTimeProvider.UtcNow), ct).ConfigureAwait(false);

            // Execute auto-response if configured
            if (policy.AutoResponse.HasValue)
            {
                await ExecuteAutoResponseAsync(
                    policy.AutoResponse.Value,
                    trace.AgentId,
                    policy.Id,
                    ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Projects the ingested trace event onto a domain Trace entity so that
    /// RuleDefinition evaluators can inspect operation fields and attributes.
    /// </summary>
    internal static global::SeldonVigil.Api.Modules.Observability.Domain.Entities.Trace ToDomainTrace(TraceIngestedEvent traceEvent)
    {
        ArgumentNullException.ThrowIfNull(traceEvent);

        var metadata = traceEvent.Metadata ?? new Dictionary<string, object>();
        var attributes = metadata.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var operationType = Enum.TryParse<OperationType>(
            attributes.GetValueOrDefault("operationType"), ignoreCase: true, out var parsedType)
            ? parsedType
            : OperationType.Custom;

        var startTime = traceEvent.Timestamp == default ? DateTime.UtcNow : traceEvent.Timestamp;
        var durationMs = double.TryParse(
            attributes.GetValueOrDefault("durationMs"), out var duration) ? duration : 0;
        var endTime = startTime.AddMilliseconds(durationMs);

        var status = Enum.TryParse<TraceStatus>(
            attributes.GetValueOrDefault("status"), ignoreCase: true, out var parsedStatus)
            ? parsedStatus
            : TraceStatus.Ok;

        var result = global::SeldonVigil.Api.Modules.Observability.Domain.Entities.Trace.Create(
            traceEvent.TenantId,
            traceEvent.AgentId,
            parentTraceId: null,
            operationName: attributes.GetValueOrDefault("operationName", operationType.ToString()),
            operationType,
            startTime,
            endTime,
            status,
            attributes,
            events: [],
            links: [],
            new TokenUsage(traceEvent.TokenUsage, 0),
            new Cost(traceEvent.Cost),
            id: traceEvent.TraceId);

        return result.IsSuccess ? result.Value! : throw new InvalidOperationException(
            $"Failed to materialise trace {traceEvent.TraceId}: {result.Error?.Message}");
    }

    private async Task ExecuteAutoResponseAsync(
        AutoResponse response,
        AgentId agentId,
        SecurityPolicyId policyId,
        CancellationToken ct)
    {
        switch (response)
        {
            case AutoResponse.SuspendAgent:
                await eventBus.PublishAsync(new AutoSuspendAgentRequestedEvent(
                    agentId, policyId, "Automatic suspension due to policy violation")
                {
                    Timestamp = DateTime.UtcNow
                }, ct)
                    .ConfigureAwait(false);
                LogAutoSuspendRequested(logger, agentId);
                break;

            case AutoResponse.BlockRequest:
                LogAutoBlockConfigured(logger, agentId);
                break;

            case AutoResponse.AlertOnly:
                LogAlertOnlyResponse(logger, agentId);
                break;

            case AutoResponse.Quarantine:
                LogAutoQuarantineRequested(logger, agentId);
                break;
        }
    }
}

file static class SecurityAnalyzerActivity
{
    private static readonly ActivitySource Source = new("SeldonVigil.Workers.SecurityAnalyzer");

    public static Activity? StartActivity(string traceId)
    {
        var activity = Source.StartActivity("security.analyze");
        activity?.SetTag("security.trace_id", traceId);
        return activity;
    }
}
