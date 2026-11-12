namespace SeldonVigil.Api.Modules.Observability.Domain.Entities;

public sealed record TraceEvent(
    string Name,
    DateTime Timestamp,
    Dictionary<string, string> Attributes);
