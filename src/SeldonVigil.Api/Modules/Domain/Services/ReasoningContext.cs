using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ReasoningContext(
    TenantId TenantId,
    AgentId? AgentId,
    ReasoningPriority Priority = ReasoningPriority.Standard,
    TimeSpan? Timeout = null,
    IReadOnlyDictionary<string, object>? Metadata = null);
