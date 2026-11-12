namespace SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;

internal sealed class TraceEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AgentId { get; set; }
    public Guid? ParentTraceId { get; set; }
    public string Framework { get; set; } = "unknown";
    public string ModelProvider { get; set; } = "unknown";
    public string ModelName { get; set; } = "unknown";
    public string OperationName { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public string Attributes { get; set; } = "{}";
    public string Events { get; set; } = "[]";
    public string Links { get; set; } = "[]";
    public int TokenUsageInput { get; set; }
    public int TokenUsageOutput { get; set; }
    public int TokenUsageTotal { get; set; }
    public int TokenUsageCached { get; set; }
    public decimal CostUsd { get; set; }
    public string CostCurrency { get; set; } = "USD";
    public double LatencyMs { get; set; }
    public double QueueTimeMs { get; set; }
    public bool IsRootSpan { get; set; }
    public bool IsAsync { get; set; }
    public byte TraceFlags { get; set; }
    public DateTime CreatedAt { get; set; }
}