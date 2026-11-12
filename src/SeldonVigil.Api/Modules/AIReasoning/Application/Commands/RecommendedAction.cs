namespace SeldonVigil.Api.Modules.AIReasoning.Application.Commands;

internal sealed record RecommendedAction(
    string Action,
    string Priority,
    string Owner);
