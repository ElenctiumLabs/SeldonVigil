using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Npgsql;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Shared.Infrastructure.VectorSearch;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// Implements a three-tier memory system:
/// - Working Memory (distributed cache): Current conversation context, TTL 1 hour
/// - Semantic Memory (IVectorStore): Long-term embeddings, vector search (pgvector)
/// - Episodic Memory (PostgreSQL): Structured events with temporal relationships
/// </summary>
internal sealed partial class MemoryService(
    ICacheService workingMemory,
    IVectorStore semanticMemory,
    NpgsqlDataSource episodicMemory,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IDateTimeProvider dateTimeProvider,
    ILogger<MemoryService> logger)
    : IMemoryService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Stored semantic memory in {Collection}: {ContentLength} chars")]
    private static partial void LogSemanticMemoryStored(ILogger logger, string collection, int contentLength);

    public async Task StoreWorkingMemoryAsync(
        string sessionId,
        string key,
        object value,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var cacheKey = $"seldonvigil:memory:working:{sessionId}:{key}";
        await workingMemory.SetAsync(cacheKey, value, ttl ?? TimeSpan.FromHours(1), ct)
            .ConfigureAwait(false);
    }

    public async Task<T?> RetrieveWorkingMemoryAsync<T>(
        string sessionId,
        string key,
        CancellationToken ct = default)
    {
        var cacheKey = $"seldonvigil:memory:working:{sessionId}:{key}";
        return await workingMemory.GetAsync<T>(cacheKey, ct).ConfigureAwait(false);
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public async Task StoreSemanticMemoryAsync(
        string collection,
        string content,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
    {
        var embedding = await embeddingGenerator.GenerateAsync(
            [content],
            cancellationToken: ct).ConfigureAwait(false);

        var stringMetadata = metadata.ToDictionary(
            x => x.Key,
            x => x.Value?.ToString() ?? "");

        await semanticMemory.UpsertAsync(
            collection,
            new VectorRecord(
                Id: Guid.NewGuid(),
                Content: content,
                Timestamp: dateTimeProvider.UtcNow,
                Tags: new Dictionary<string, string>(stringMetadata),
                Metadata: stringMetadata,
                Vector: embedding[0].Vector.ToArray()),
            ct).ConfigureAwait(false);

        LogSemanticMemoryStored(logger, collection, content.Length);
    }

    public async Task<IReadOnlyList<SemanticSearchResult>> SearchSemanticMemoryAsync(
        string collection,
        string query,
        int limit = 10,
        Dictionary<string, string>? filters = null,
        CancellationToken ct = default)
    {
        var embedding = await embeddingGenerator.GenerateAsync(
            [query],
            cancellationToken: ct).ConfigureAwait(false);

        var results = await semanticMemory.SearchAsync(
            collection,
            new ReadOnlyMemory<float>(embedding[0].Vector.ToArray()),
            limit,
            filters,
            ct).ConfigureAwait(false);

        return results.Select(r => new SemanticSearchResult(
            r.Id.ToString(),
            r.Content,
            r.Score,
            JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(r.Metadata)) ?? new Dictionary<string, object>(),
            r.Timestamp.DateTime
        )).ToList().AsReadOnly();
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public async Task StoreEpisodicMemoryAsync(
        AgentId agentId,
        string eventType,
        string description,
        Dictionary<string, object> context,
        CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO episodic_memories (
                id, agent_id, event_type, description, context,
                temporal_links, created_at
            ) VALUES (
                @id, @agentId, @eventType, @description,
                @context::jsonb, @temporalLinks::jsonb, @createdAt
            )";

        await using var command = episodicMemory.CreateCommand(sql);
        var cmd = command;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("agentId", agentId.Value);
        cmd.Parameters.AddWithValue("eventType", eventType);
        cmd.Parameters.AddWithValue("description", description);
        cmd.Parameters.AddWithValue("context", JsonSerializer.Serialize(context));
        cmd.Parameters.AddWithValue("temporalLinks", "[]"); // Would link to previous/future events
        cmd.Parameters.AddWithValue("createdAt", dateTimeProvider.UtcNow);

        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public async Task<IReadOnlyList<EpisodicMemory>> RetrieveEpisodicSequenceAsync(
        AgentId agentId,
        DateTime from,
        DateTime to,
        string? eventType = null,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT id, event_type, description, context, temporal_links, created_at
            FROM episodic_memories
            WHERE agent_id = @agentId
              AND created_at BETWEEN @from AND @to";

        if (!string.IsNullOrEmpty(eventType))
        {
            sql += " AND event_type = @eventType";
        }

        sql += " ORDER BY created_at";

        await using var command = episodicMemory.CreateCommand(sql);
        var cmd = command;
        cmd.Parameters.AddWithValue("agentId", agentId.Value);
        cmd.Parameters.AddWithValue("from", from);
        cmd.Parameters.AddWithValue("to", to);

        if (!string.IsNullOrEmpty(eventType))
        {
            cmd.Parameters.AddWithValue("eventType", eventType);
        }

        var memories = new List<EpisodicMemory>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            memories.Add(new EpisodicMemory(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(3)) ?? new Dictionary<string, object>(),
                JsonSerializer.Deserialize<List<Guid>>(reader.GetString(4)) ?? [],
                reader.GetDateTime(5)));
        }

        return memories.AsReadOnly();
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer")]
    public async Task StoreAgentMemoryAsync(
        AgentId agentId,
        TenantId tenantId,
        string content,
        MemoryType type,
        Dictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        // Delegate to working/episodic memory for now
        // In production, would use vector database
        await StoreEpisodicMemoryAsync(
            agentId,
            type.ToString(),
            content,
            metadata.ToDictionary(x => x.Key, x => (object)x.Value),
            ct).ConfigureAwait(false);
    }

    [RequiresDynamicCode("Calls SeldonVigil.Api.Modules.Domain.Services.MemoryService.RetrieveEpisodicSequenceAsync(AgentId, DateTime, DateTime, String, CancellationToken)")]
    public async Task<IReadOnlyList<MemoryResult>> SearchAgentMemoryAsync(
        AgentId agentId,
        TenantId tenantId,
        string query,
        int limit = 10,
        CancellationToken ct = default)
    {
        // Retrieve from episodic memory
        var memories = await RetrieveEpisodicSequenceAsync(
            agentId,
            dateTimeProvider.UtcNow.AddDays(-30),
            dateTimeProvider.UtcNow,
            ct: ct).ConfigureAwait(false);

        return memories
            .Take(limit)
            .Select(m => new MemoryResult(m.Id.ToString(), m.Description, 0.9f, m.CreatedAt, m.Context.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? ""), m.TemporalLinks))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<HistoricalPattern>> GetBehavioralPatternsAsync(
        AgentId agentId,
        TenantId tenantId,
        int limit = 5,
        CancellationToken ct = default)
    {
        // Return empty for now - would analyze patterns from memory
        await Task.CompletedTask.ConfigureAwait(false);
        return new List<HistoricalPattern>().AsReadOnly();
    }
}
