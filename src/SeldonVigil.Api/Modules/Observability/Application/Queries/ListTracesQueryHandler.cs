using SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Observability.Application.Queries;

// Query definitions
internal sealed record ListTracesQuery(
    Guid? AgentId = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50) : IQuery<IReadOnlyList<TraceDto>>;

internal sealed record GetTraceByIdQuery(Guid Id) : IQuery<TraceDto>;

internal sealed record TraceDto(
    Guid Id,
    Guid AgentId,
    string Status,
    DateTime CreatedAt,
    int DurationMs,
    string? Error,
    decimal CostUsd,
    string OperationName,
    string OperationType,
    string ModelName,
    Guid? ParentTraceId,
    int TotalTokens,
    decimal CostUsdTotal,
    string AgentName,
    string? StatusMessage,
    Dictionary<string, string>? Attributes,
    List<TraceEventDto>? Events,
    IReadOnlyList<TraceDto>? Children = null);

internal sealed class ListTracesQueryHandler(ITraceStore traceStore)
    : IQueryHandler<ListTracesQuery, IReadOnlyList<TraceDto>>
{
    public async Task<Shared.Domain.Result<IReadOnlyList<TraceDto>>> Handle(
        ListTracesQuery query,
        CancellationToken cancellationToken)
    {
        var summaries = await traceStore.SearchTracesAsync(
            query.AgentId,
            query.Status,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            cancellationToken).ConfigureAwait(false);

        var traces = summaries
            .Select(MapToDto)
            .ToList();

        return Shared.Domain.Result<IReadOnlyList<TraceDto>>.Success((IReadOnlyList<TraceDto>)traces);
    }

    internal static TraceDto MapToDto(TraceSummaryDto summary) => new(
        summary.TraceId.Value,
        summary.AgentId,
        summary.StatusCode,
        summary.CreatedAt,
        (int)Math.Round(summary.DurationMs),
        summary.StatusMessage,
        summary.CostUsd,
        summary.OperationName,
        summary.OperationType,
        summary.ModelName,
        summary.ParentTraceId,
        summary.TotalTokens,
        summary.CostUsd,
        summary.AgentName ?? summary.AgentId.ToString(),
        summary.StatusMessage,
        summary.Attributes,
        summary.Events);
}

internal sealed class GetTraceByIdQueryHandler(ITraceStore traceStore)
    : IQueryHandler<GetTraceByIdQuery, TraceDto>
{
    public async Task<Shared.Domain.Result<TraceDto>> Handle(
        GetTraceByIdQuery query,
        CancellationToken cancellationToken)
    {
        var summary = await traceStore.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);

        if (summary is null)
        {
            return Shared.Domain.Result<TraceDto>.Failure(Error.NotFound(
                "Trace.NotFound",
                $"Trace '{query.Id}' was not found."));
        }

        var childrenSummaries = await traceStore.FindChildrenAsync(query.Id, cancellationToken).ConfigureAwait(false);
        var children = childrenSummaries.Select(ListTracesQueryHandler.MapToDto).ToList();

        var rootDto = ListTracesQueryHandler.MapToDto(summary) with { Children = children };

        return Shared.Domain.Result<TraceDto>.Success(rootDto);
    }
}
