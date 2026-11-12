namespace SeldonVigil.Api.Modules.Domain.Agents;

internal sealed record RCAAction(
    string Action,
    string Priority,
    string Owner);
