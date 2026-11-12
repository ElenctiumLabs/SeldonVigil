namespace SeldonVigil.Api.Modules.Observability.Domain.Entities;

public sealed record Cost(
    decimal Amount,
    string Currency = "USD");
