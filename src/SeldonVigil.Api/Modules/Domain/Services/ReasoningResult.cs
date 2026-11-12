namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ReasoningResult<TResponse>
{
    public required Guid ChainId { get; init; }
    public TResponse? Response { get; init; }
    public IReadOnlyList<ReasoningStep> ReasoningSteps { get; init; } = [];
    public IReadOnlyList<ToolCallRecord> ToolCalls { get; init; } = [];
    public long TotalDurationMs { get; init; }
    public int TotalTokens { get; init; }
    public EvaluationResult? Evaluation { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
}
