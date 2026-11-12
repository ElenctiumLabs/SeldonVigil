namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record MemoryContext(
    IReadOnlyList<MemoryResult> RelevantMemories,
    IReadOnlyList<HistoricalPattern> Patterns,
    IReadOnlyDictionary<string, object> WorkingMemory);
