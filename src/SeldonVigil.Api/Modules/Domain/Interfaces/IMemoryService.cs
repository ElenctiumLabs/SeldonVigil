using SeldonVigil.Api.Modules.Domain.Services;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface IMemoryService
{
    Task StoreWorkingMemoryAsync(string sessionId, string key, object value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task<T?> RetrieveWorkingMemoryAsync<T>(string sessionId, string key, CancellationToken ct = default);
    Task StoreSemanticMemoryAsync(string collection, string content, Dictionary<string, object> metadata, CancellationToken ct = default);
    Task<IReadOnlyList<SemanticSearchResult>> SearchSemanticMemoryAsync(string collection, string query, int limit = 10, Dictionary<string, string>? filters = null, CancellationToken ct = default);
    Task StoreEpisodicMemoryAsync(AgentId agentId, string eventType, string description, Dictionary<string, object> context, CancellationToken ct = default);
    Task<IReadOnlyList<EpisodicMemory>> RetrieveEpisodicSequenceAsync(AgentId agentId, DateTime from, DateTime to, string? eventType = null, CancellationToken ct = default);
    Task StoreAgentMemoryAsync(AgentId agentId, TenantId tenantId, string content, MemoryType type, Dictionary<string, string> metadata, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryResult>> SearchAgentMemoryAsync(AgentId agentId, TenantId tenantId, string query, int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<HistoricalPattern>> GetBehavioralPatternsAsync(AgentId agentId, TenantId tenantId, int limit = 5, CancellationToken ct = default);
}
