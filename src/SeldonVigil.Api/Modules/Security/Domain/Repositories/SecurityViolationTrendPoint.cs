namespace SeldonVigil.Api.Modules.Security.Domain.Repositories;

internal sealed record SecurityViolationTrendPoint(DateOnly Date, int Count);
