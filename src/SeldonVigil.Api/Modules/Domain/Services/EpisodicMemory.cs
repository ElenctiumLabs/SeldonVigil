namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record EpisodicMemory(
    Guid Id,
    string EventType,
    string Description,
    Dictionary<string, object> Context,
    IReadOnlyList<Guid> TemporalLinks,
    DateTime CreatedAt);
