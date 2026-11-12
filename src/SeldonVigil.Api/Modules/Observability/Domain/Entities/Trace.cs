using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Observability.Domain.Entities;

public sealed class Trace : BaseEntity<TraceId>
{
    public TenantId TenantId { get; private set; }
    public AgentId AgentId { get; private set; }
    public TraceId? ParentTraceId { get; private set; }
    public string OperationName { get; private set; } = string.Empty;
    public OperationType OperationType { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public TimeSpan Duration => EndTime - StartTime;
    public TraceStatus Status { get; private set; }
    public string? StatusMessage { get; }

    public IReadOnlyDictionary<string, string> Attributes { get; private set; } = new Dictionary<string, string>();
    public IReadOnlyList<TraceEvent> Events { get; private set; } = [];
    public IReadOnlyList<TraceLink> Links { get; private set; } = [];

    public TokenUsage TokenUsage { get; private set; } = null!;
    public Cost Cost { get; private set; } = null!;

    public bool IsRootSpan => ParentTraceId is null;
    public bool IsAsync { get; private set; }
    public string ModelName { get; private set; } = "unknown";

    private Trace() { }

    public static Result<Trace> Create(
        TenantId tenantId,
        AgentId agentId,
        TraceId? parentTraceId,
        string operationName,
        OperationType operationType,
        DateTime startTime,
        DateTime endTime,
        TraceStatus status,
        Dictionary<string, string> attributes,
        List<TraceEvent> events,
        List<TraceLink> links,
        TokenUsage tokenUsage,
        Cost cost,
        bool isAsync = false,
        TraceId? id = null,
        string modelName = "unknown")
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            return Result<Trace>.Failure(Error.Validation(
                "Trace.InvalidOperationName",
                "Operation name is required."));
        }

        if (endTime < startTime)
        {
            return Result<Trace>.Failure(Error.Validation(
                "Trace.InvalidTiming",
                "End time must be after start time."));
        }

        var trace = new Trace
        {
            Id = id ?? TraceId.New(),
            TenantId = tenantId,
            AgentId = agentId,
            ParentTraceId = parentTraceId,
            OperationName = operationName,
            OperationType = operationType,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            Attributes = attributes,
            Events = events,
            Links = links,
            TokenUsage = tokenUsage,
            Cost = cost,
            IsAsync = isAsync,
            ModelName = modelName,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Trace>.Success(trace);
    }

    public void AddEvent(TraceEvent traceEvent)
    {
        var events = Events.ToList();
        events.Add(traceEvent);
        Events = events.AsReadOnly();
    }

    public bool HasSilentFailure()
    {
        // Silent failure: agent declared tools but didn't use them, or fabricated results
        if (OperationType != OperationType.ToolCall)
        {
            return false;
        }

        var declaredTools = Events
            .Where(e => e.Name == "tool.declared")
            .Select(e => e.Attributes.GetValueOrDefault("tool_name"))
            .ToHashSet();

        var executedTools = Events
            .Where(e => e.Name == "tool.executed")
            .Select(e => e.Attributes.GetValueOrDefault("tool_name"))
            .ToHashSet();

        // If declared tools exist but none were executed → silent failure
        return declaredTools.Count > 0 && executedTools.Count == 0;
    }
}
