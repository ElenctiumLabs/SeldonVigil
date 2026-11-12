using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Queries;

internal sealed record ListAgentsQuery(
    AgentStatus? Status = null,
    AgentFramework? Framework = null,
    ModelProvider? ModelProvider = null,
    int Page = 1,
    int PageSize = 50
) : IQuery<PaginatedResult<AgentListItemDto>>;
