using System.Collections.Concurrent;
using System.Text.Json;
using Npgsql;

namespace SeldonVigil.Api.Shared.Infrastructure.VectorSearch;

/// <summary>
/// Default IVectorStore backed by the pgvector extension on the existing
/// PostgreSQL instance. No additional service to operate; backups, tenancy
/// and connection pooling are inherited from the primary database.
///
/// Table layout (one row per vector):
///   vector_memories(id uuid PK, collection text, content text, timestamp timestamptz,
///                   metadata jsonb, tags jsonb, embedding vector(dim))
/// Equality filters map to jsonb containment on <c>tags</c>.
/// </summary>
internal sealed partial class PgVectorStore(
    NpgsqlDataSource dataSource,
    Microsoft.Extensions.Configuration.IConfiguration configuration,
    ILogger<PgVectorStore> logger)
    : IVectorStore
{
    private readonly ConcurrentDictionary<int, bool> _schemaReady = new();

    [LoggerMessage(Level = LogLevel.Information, Message = "pgvector schema ensured for dimension {Dimensions} in collection {Collection}")]
    private static partial void LogSchemaEnsured(ILogger logger, int dimensions, string collection);

    [LoggerMessage(Level = LogLevel.Error, Message = "pgvector extension is not available on the target PostgreSQL instance. Install pgvector to enable vector search.")]
    private static partial void LogExtensionMissing(ILogger logger);

    private int Dimensions => configuration.GetValue("Vectors:Dimensions", 3072);

    /// <summary>Idempotently creates the extension, table and ANN index.</summary>
    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        var dimensions = Dimensions;
        if (_schemaReady.TryGetValue(dimensions, out var ready) && ready)
        {
            return;
        }

        await using var cmd = dataSource.CreateCommand($$"""
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS vector_memories (
                id         uuid PRIMARY KEY,
                collection text NOT NULL,
                content    text NOT NULL,
                timestamp  timestamptz NOT NULL DEFAULT now(),
                metadata   jsonb NOT NULL DEFAULT '{}'::jsonb,
                tags       jsonb NOT NULL DEFAULT '{}'::jsonb,
                embedding  vector({{dimensions}}) NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_vector_memories_collection ON vector_memories (collection);
            CREATE INDEX IF NOT EXISTS ix_vector_memories_embedding
                ON vector_memories USING hnsw (embedding vector_cosine_ops);
            """);
        try
        {
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            _schemaReady[dimensions] = true;
            LogSchemaEnsured(logger, dimensions, "*");
        }
        catch (PostgresException ex) when (ex.SqlState == "0A000" || ex.Message.Contains("extension", StringComparison.OrdinalIgnoreCase))
        {
            LogExtensionMissing(logger);
            throw new InvalidOperationException(
                "The 'vector' extension (pgvector) is not installed on this PostgreSQL server. " +
                "Install it (e.g. 'CREATE EXTENSION vector;' with the pgvector package).", ex);
        }
    }

    public async Task UpsertAsync(
        string collection,
        VectorRecord record,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        const string sql = """
            INSERT INTO vector_memories (id, collection, content, timestamp, metadata, tags, embedding)
            VALUES (@id, @collection, @content, @timestamp, @metadata::jsonb, @tags::jsonb, @embedding::vector)
            ON CONFLICT (id) DO UPDATE SET
                collection = EXCLUDED.collection,
                content    = EXCLUDED.content,
                timestamp  = EXCLUDED.timestamp,
                metadata   = EXCLUDED.metadata,
                tags       = EXCLUDED.tags,
                embedding  = EXCLUDED.embedding
            """;

        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", record.Id);
        cmd.Parameters.AddWithValue("collection", collection);
        cmd.Parameters.AddWithValue("content", record.Content);
        cmd.Parameters.AddWithValue("timestamp", record.Timestamp);
        cmd.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(record.Metadata));
        cmd.Parameters.AddWithValue("tags", JsonSerializer.Serialize(record.Tags));
        cmd.Parameters.AddWithValue("embedding", ToPgVectorLiteral(record.Vector));

        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collection,
        ReadOnlyMemory<float> queryVector,
        int limit,
        IReadOnlyDictionary<string, string>? tagFilters = null,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);

        // Cosine distance <=> ; score normalized to similarity in [0..1] as 1 - distance.
        var sql = """
            SELECT id, content, timestamp, metadata::text,
                   1 - (embedding <=> @query::vector) AS score
            FROM vector_memories
            WHERE collection = @collection
              AND tags @> @tags::jsonb
            ORDER BY embedding <=> @query::vector
            LIMIT @limit
            """;

        var tagsJson = JsonSerializer.Serialize(tagFilters ?? new Dictionary<string, string>());

        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("collection", collection);
        cmd.Parameters.AddWithValue("tags", tagsJson);
        cmd.Parameters.AddWithValue("query", ToPgVectorLiteral(queryVector.ToArray()));
        cmd.Parameters.AddWithValue("limit", limit);

        var results = new List<VectorSearchResult>(limit);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(new VectorSearchResult(
                Id: reader.GetGuid(0),
                Content: reader.GetString(1),
                Score: (float)reader.GetDouble(4),
                Timestamp: reader.GetFieldValue<DateTimeOffset>(2),
                Metadata: JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(3))
                    ?? new Dictionary<string, string>()));
        }

        return results.AsReadOnly();
    }

    /// <summary>Formats floats as a pgvector text literal: '[1,2,3]' (cast at the parameter site).</summary>
    private static string ToPgVectorLiteral(ReadOnlySpan<float> vector)
    {
        var sb = new System.Text.StringBuilder(vector.Length * 10);
        sb.Append('[');
        for (var i = 0; i < vector.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(vector[i].ToString("G9", System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
