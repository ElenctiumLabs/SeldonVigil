namespace SeldonVigil.Api.Modules.Observability.Domain.Entities;

public enum TraceStatus
{
    Ok,
    Error,
    Degraded,
    Cancelled
}
