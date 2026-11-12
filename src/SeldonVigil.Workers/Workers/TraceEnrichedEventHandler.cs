using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Workers.Workers;

internal sealed partial class TraceEnrichedEventHandler(
    ILogger<TraceEnrichedEventHandler> logger)
    : IEventHandler<TraceEnrichedEvent>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Trace enriched: {TraceId} agent={AgentId} cost={Cost:F4} tokens={TokenUsage}")]
    private static partial void LogTraceEnriched(ILogger logger, TraceId traceId, AgentId agentId, decimal cost, int tokenUsage);

    public Task HandleAsync(TraceEnrichedEvent @event, CancellationToken ct = default)
    {
        LogTraceEnriched(logger, @event.TraceId, @event.AgentId, @event.Cost, @event.TokenUsage);

        return Task.CompletedTask;
    }
}
