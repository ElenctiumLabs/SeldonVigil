using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Application.Queries;

internal sealed record AgentListItemDto(
    AgentId Id,
    string ExternalId,
    string Name,
    string Framework,
    string ModelProvider,
    string ModelName,
    string Status,
    DateTime CreatedAt);
