using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;

namespace SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;

internal interface ITraceStore
{
    Task StoreAsync(Trace trace, CancellationToken ct = default);
    Task<IReadOnlyList<TraceSummaryDto>> GetAgentTracesAsync(AgentId agentId, DateTime from, DateTime to, int limit, CancellationToken ct = default);

    /// <summary>Filterable, paginated trace search backing GET /observability/traces.</summary>
    Task<IReadOnlyList<TraceSummaryDto>> SearchTracesAsync(
        Guid? agentId,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Single-trace lookup backing GET /observability/traces/{id}.</summary>
    Task<TraceSummaryDto?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Finds child traces/spans belonging to a parent trace.</summary>
    Task<IReadOnlyList<TraceSummaryDto>> FindChildrenAsync(Guid parentTraceId, CancellationToken ct = default);
}
