using SeldonVigil.Api.Modules.Identity.Domain.Enums;

namespace SeldonVigil.Api.Modules.Identity.Api.Endpoints;

internal sealed record ListAgentsRequest(
    AgentStatus? Status,
    AgentFramework? Framework,
    ModelProvider? ModelProvider,
    int Page = 1,
    int PageSize = 50);
