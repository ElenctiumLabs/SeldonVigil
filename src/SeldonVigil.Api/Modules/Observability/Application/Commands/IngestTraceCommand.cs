using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Observability.Application.Commands;

internal sealed record IngestTraceCommand(
    TenantId TenantId,
    AgentId AgentId,
    TraceId? ParentTraceId,
    string OperationName,
    OperationType OperationType,
    DateTime StartTime,
    DateTime EndTime,
    TraceStatus Status,
    Dictionary<string, string> Attributes,
    List<TraceEvent> Events,
    List<TraceLink> Links,
    TokenUsage TokenUsage,
    Cost Cost,
    bool IsAsync = false,
    TraceId? Id = null,
    string ModelName = "unknown"
) : ICommand<TraceId>;
