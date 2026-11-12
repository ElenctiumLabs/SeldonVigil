using SeldonVigil.Api.Modules.Observability.Domain.Entities;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record EvaluateSecurityRequest(
    Guid TraceId,
    Guid AgentId,
    Dictionary<string, string> Attributes,
    string OperationName,
    OperationType OperationType,
    decimal CostUsd,
    int TokenUsageTotal);
