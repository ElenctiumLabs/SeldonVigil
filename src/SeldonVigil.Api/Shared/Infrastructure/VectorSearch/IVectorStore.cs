namespace SeldonVigil.Api.Shared.Infrastructure.VectorSearch;

/// <summary>
/// Vector search contract. The implementation is pgvector on the primary
/// Postgres; swap this registration to change backend.
/// </summary>
public interface IVectorStore
{
    Task UpsertAsync(
        string collection,
        VectorRecord record,
        CancellationToken ct = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collection,
        ReadOnlyMemory<float> queryVector,
        int limit,
        IReadOnlyDictionary<string, string>? tagFilters = null,
        CancellationToken ct = default);
}

public sealed record VectorRecord(
    Guid Id,
    string Content,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    float[] Vector);

public sealed record VectorSearchResult(
    Guid Id,
    string Content,
    float Score,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Metadata);
