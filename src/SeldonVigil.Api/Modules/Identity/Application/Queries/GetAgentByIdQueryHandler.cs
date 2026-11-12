using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Application.Queries;

internal sealed partial class GetAgentByIdQueryHandler(
    IAgentRepository agentRepository,
    ICacheService cacheService,
    ILogger<GetAgentByIdQueryHandler> logger)
    : IQueryHandler<GetAgentByIdQuery, AgentDto>
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Agent cache hit: {AgentId}")]
    private static partial void LogAgentCacheHit(ILogger logger, AgentId agentId);

    public async Task<Shared.Domain.Result<AgentDto>> Handle(GetAgentByIdQuery request, CancellationToken ct)
    {
        var cacheKey = $"seldonvigil:identity:{request.AgentId}";

        var cached = await cacheService.GetAsync<AgentDto>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            LogAgentCacheHit(logger, request.AgentId);
            return Result.Success(cached);
        }

        var agent = await agentRepository.GetByIdAsync(request.AgentId, ct).ConfigureAwait(false);
        if (agent is null)
        {
            return Shared.Domain.Result<AgentDto>.Failure(Error.NotFound(
                "Agent.NotFound",
                $"Agent with ID '{request.AgentId}' was not found."));
        }

        var dto = MapToDto(agent);

        await cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct)
            .ConfigureAwait(false);

        return Result.Success(dto);
    }

    private static AgentDto MapToDto(Agent agent) => new(
        agent.Id,
        agent.ExternalId,
        agent.Name,
        agent.Description,
        agent.Framework.ToString(),
        agent.FrameworkVersion,
        agent.ModelProvider.ToString(),
        agent.ModelName,
        agent.ModelVersion,
        agent.Status.ToString(),
        agent.CapabilityManifest,
        agent.IdentityTokenJti,
        agent.IdentityTokenExpiresAt,
        agent.SourceVersion,
        agent.SourceCodeHash,
        agent.PromptTemplateHash,
        agent.CreatedAt,
        agent.UpdatedAt,
        agent.ActivatedAt,
        agent.SuspendedAt,
        agent.SuspendedReason,
        agent.RevokedAt,
        agent.RevokedReason);
}
