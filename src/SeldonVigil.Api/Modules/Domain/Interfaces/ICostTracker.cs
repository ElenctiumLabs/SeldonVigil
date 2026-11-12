namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface ICostTracker
{
    void TrackUsage(Guid chainId, int inputTokenCount, int outputTokenCount, string modelId);

    Task<IReadOnlyList<CostUsageSnapshot>> GetRecentUsageAsync(string modelId, int limit = 50, CancellationToken ct = default);

    Task<CostAggregate> GetAggregateAsync(string modelId, DateTime from, DateTime to, CancellationToken ct = default);

    Task EnsureTableExistsAsync(CancellationToken ct = default);
}

internal sealed record CostUsageSnapshot(
    Guid ChainId,
    int InputTokens,
    int OutputTokens,
    string ModelId,
    DateTime RecordedAt);

internal sealed record CostAggregate(
    string ModelId,
    long TotalInputTokens,
    long TotalOutputTokens,
    long TotalCalls,
    DateTime From,
    DateTime To);
