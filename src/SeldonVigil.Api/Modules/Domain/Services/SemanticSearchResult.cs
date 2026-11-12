namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record SemanticSearchResult(
    string Id,
    string Content,
    float Score,
    Dictionary<string, object> Metadata,
    DateTime Timestamp);
