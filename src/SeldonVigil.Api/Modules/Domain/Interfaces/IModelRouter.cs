using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface IModelRouter
{
    string SelectModel(TaskComplexity complexity, double requiredConfidence, decimal? budgetLimit, string? preferredProvider = null);
    Task<T> RouteWithFallbackAsync<T>(TaskComplexity complexity, double requiredConfidence, Func<string, CancellationToken, Task<T>> executeAsync, CancellationToken ct);
}
