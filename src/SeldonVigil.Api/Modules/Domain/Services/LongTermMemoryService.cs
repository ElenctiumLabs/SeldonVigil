using System.Diagnostics.CodeAnalysis;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.VectorSearch;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// Agent long-term memory over the platform vector store (pgvector on the
/// primary Postgres). Embeddings via IEmbeddingGenerator.
/// </summary>
internal sealed partial class LongTermMemoryService(
    IVectorStore vectorStore,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    ILogger<LongTermMemoryService> logger)
{
    private const string CollectionName = "agent_memory";

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stored memory for agent {AgentId}: {ContentLength} chars")]
    private static partial void LogMemoryStored(ILogger logger, AgentId agentId, int contentLength);

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public async Task StoreAgentMemoryAsync(
        AgentId agentId,
        TenantId tenantId,
        string content,
        MemoryType type,
        Dictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var embedding = await embeddingGenerator.GenerateAsync(
            [content],
            cancellationToken: ct).ConfigureAwait(false);

        await vectorStore.UpsertAsync(
            CollectionName,
            new VectorRecord(
                Id: Guid.NewGuid(),
                Content: content,
                Timestamp: DateTimeOffset.UtcNow,
                Tags: new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString(),
                    ["agent_id"] = agentId.ToString(),
                    ["type"] = type.ToString()
                },
                Metadata: metadata,
                Vector: embedding[0].Vector.ToArray()),
            ct).ConfigureAwait(false);

        LogMemoryStored(logger, agentId, content.Length);
    }

    public async Task<IReadOnlyList<MemoryResult>> SearchAgentMemoryAsync(
        AgentId agentId,
        TenantId tenantId,
        string query,
        int limit = 10,
        CancellationToken ct = default)
    {
        var embedding = await embeddingGenerator.GenerateAsync(
            [query],
            cancellationToken: ct).ConfigureAwait(false);

        var results = await vectorStore.SearchAsync(
            CollectionName,
            new ReadOnlyMemory<float>(embedding[0].Vector.ToArray()),
            limit,
            new Dictionary<string, string>
            {
                ["tenant_id"] = tenantId.ToString(),
                ["agent_id"] = agentId.ToString()
            },
            ct).ConfigureAwait(false);

        return results.Select(r => new MemoryResult(
            r.Id.ToString(),
            r.Content,
            r.Score,
            r.Timestamp.DateTime,
            r.Metadata
        )).ToList().AsReadOnly();
    }
}
