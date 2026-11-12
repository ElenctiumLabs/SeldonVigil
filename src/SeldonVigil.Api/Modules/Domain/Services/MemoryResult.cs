namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record MemoryResult(
    string Id,
    string Content,
    float Score,
    DateTime Timestamp,
    Dictionary<string, string> Metadata,
    IReadOnlyList<Guid>? TemporalLinks = null);
