using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;
using IRequestContext = SeldonVigil.Api.Shared.Application.IRequestContext;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed partial class RevokeAgentCommandHandler(
    IAgentRepository agentRepository,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider,
    IRequestContext requestContext,
    ICacheService cacheService,
    ILogger<RevokeAgentCommandHandler> logger)
    : Shared.Application.ICommandHandler<RevokeAgentCommand>
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent revoked: {AgentId}. Reason: {Reason}")]
    private static partial void LogAgentRevoked(ILogger logger, AgentId agentId, string reason);

    public async Task<Shared.Domain.Result<Unit>> Handle(RevokeAgentCommand request, CancellationToken ct)
    {
        var agent = await agentRepository.GetByIdAsync(request.AgentId, ct).ConfigureAwait(false);
        if (agent is null)
        {
            return Shared.Domain.Result<Unit>.Failure(Error.NotFound(
                "Agent.NotFound",
                $"Agent with ID '{request.AgentId}' was not found."));
        }

        var result = agent.Revoke(request.Reason, requestContext.UserId ?? Guid.Empty, dateTimeProvider);
        if (result.IsFailure)
        {
            return Shared.Domain.Result<Unit>.Failure(result.Error!);
        }

        await agentRepository.UpdateAsync(agent, ct).ConfigureAwait(false);

        // Align cache key with GetAgentByIdQueryHandler and IngestTraceCommandHandler
        await cacheService.RemoveAsync($"seldonvigil:identity:{request.AgentId}", ct)
            .ConfigureAwait(false);

        foreach (var domainEvent in agent.DomainEvents)
        {
            await eventBus.PublishAsync(domainEvent, ct).ConfigureAwait(false);
        }
        agent.ClearDomainEvents();

        LogAgentRevoked(logger, request.AgentId, request.Reason);

        return Result.Success(Unit.Value);
    }
}
