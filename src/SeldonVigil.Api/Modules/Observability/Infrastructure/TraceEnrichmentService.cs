using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Observability.Infrastructure;

public sealed partial class TraceEnrichmentService(
    IDateTimeProvider dateTimeProvider,
    ILogger<TraceEnrichmentService> logger) : ITraceEnrichmentService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Enriched trace {TraceId} for agent {AgentId}")]
    private static partial void LogTraceEnriched(ILogger logger, string traceId, string agentId);

    public Task<TraceIngestedEvent> EnrichAsync(TraceIngestedEvent trace, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trace);

        ct.ThrowIfCancellationRequested();

        var metadata = new Dictionary<string, object>(trace.Metadata ?? new Dictionary<string, object>())
        {
            ["enriched_at"] = dateTimeProvider.UtcNowOffset,
            ["enrichment_version"] = "1.0"
        };

        LogTraceEnriched(logger, trace.TraceId.ToString(), trace.AgentId.ToString());

        return Task.FromResult(trace with { Metadata = metadata });
    }
}