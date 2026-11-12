namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ReasoningStep(
    int Iteration,
    string Thought,
    int TokenCount,
    long ElapsedMs);
