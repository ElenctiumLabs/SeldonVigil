using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Application.Queries;

internal sealed class ListAgentsQueryHandler(
    IAgentRepository agentRepository,
    IRequestContext requestContext)
    : IQueryHandler<ListAgentsQuery, PaginatedResult<AgentListItemDto>>
{
    public async Task<Shared.Domain.Result<PaginatedResult<AgentListItemDto>>> Handle(
        ListAgentsQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return Shared.Domain.Result<PaginatedResult<AgentListItemDto>>.Failure(Error.Unauthorized(
                "Tenant.Required",
                "Tenant context is required."));
        }

        var tenantId = new TenantId(Guid.Parse(requestContext.TenantId));

        IReadOnlyList<Agent> agents;
        if (request.Framework.HasValue)
        {
            agents = await agentRepository.GetByFrameworkAsync(tenantId, request.Framework.Value, ct)
                .ConfigureAwait(false);
        }
        else
        {
            agents = await agentRepository.GetByTenantAsync(tenantId, request.Status, ct)
                .ConfigureAwait(false);
        }

        var totalCount = agents.Count;
        var pagedItems = agents
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AgentListItemDto(
                a.Id,
                a.ExternalId,
                a.Name,
                a.Framework.ToString(),
                a.ModelProvider.ToString(),
                a.ModelName,
                a.Status.ToString(),
                a.CreatedAt))
            .ToList();

        var result = new PaginatedResult<AgentListItemDto>(
            pagedItems,
            totalCount,
            request.Page,
            request.PageSize);

        return Result.Success(result);
    }
}
