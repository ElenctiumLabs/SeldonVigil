using SeldonVigil.Api.Modules.Observability.Domain.Events;

namespace SeldonVigil.Api.Modules.Observability.Infrastructure;

public interface ITraceEnrichmentService
{
    Task<TraceIngestedEvent> EnrichAsync(TraceIngestedEvent trace, CancellationToken ct = default);
}
