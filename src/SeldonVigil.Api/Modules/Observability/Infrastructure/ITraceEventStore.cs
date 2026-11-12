using SeldonVigil.Api.Modules.Observability.Domain.Events;

namespace SeldonVigil.Api.Modules.Observability.Infrastructure;

public interface ITraceEventStore
{
    Task StoreAsync(TraceIngestedEvent trace, CancellationToken ct = default);
}