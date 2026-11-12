using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Observability.Domain.Entities;

public sealed record TraceLink(
    TraceId TraceId,
    TraceId SpanId,
    Dictionary<string, string> Attributes);
