using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed record EvaluateSecurityPolicyCommand(
    TraceId TraceId,
    AgentId AgentId,
    Dictionary<string, string> Attributes,
    string OperationName,
    OperationType OperationType,
    decimal CostUsd,
    int TokenUsageTotal
) : ICommand;
