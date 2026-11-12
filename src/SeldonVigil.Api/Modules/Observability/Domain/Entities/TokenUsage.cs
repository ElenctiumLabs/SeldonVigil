namespace SeldonVigil.Api.Modules.Observability.Domain.Entities;

public sealed record TokenUsage(
    int InputTokens,
    int OutputTokens,
    int CachedTokens = 0)
{
    public int TotalTokens => InputTokens + OutputTokens;
}
