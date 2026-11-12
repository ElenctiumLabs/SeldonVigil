using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;

internal sealed record TraceSummaryDto(
    TraceId TraceId,
    string ModelName,
    string OperationName,
    string OperationType,
    DateTime StartTime,
    DateTime EndTime,
    double DurationMs,
    string StatusCode,
    int TotalTokens,
    decimal CostUsd,
    Guid AgentId,
    DateTime CreatedAt,
    string? StatusMessage = null,
    Guid? ParentTraceId = null,
    string? AgentName = null,
    Dictionary<string, string>? Attributes = null,
    List<TraceEventDto>? Events = null);

internal sealed record TraceEventDto(string Name, string Value);
