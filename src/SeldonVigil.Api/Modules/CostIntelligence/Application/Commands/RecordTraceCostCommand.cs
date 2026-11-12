using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.CostIntelligence.Application.Commands;

internal sealed record RecordTraceCostCommand(
    TenantId TenantId,
    TraceId TraceId,
    AgentId AgentId,
    ModelProvider Provider,
    string ModelName,
    int InputTokens,
    int OutputTokens,
    int CachedTokens,
    decimal CostUsd
) : ICommand;
