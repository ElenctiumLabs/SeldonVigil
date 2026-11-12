using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed partial class RegisterAgentCommandHandler(
    IAgentRepository agentRepository,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider,
    IRequestContext requestContext,
    ILogger<RegisterAgentCommandHandler> logger)
    : ICommandHandler<RegisterAgentCommand, AgentId>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Agent registered: {AgentId} (ExternalId: {ExternalId}) for tenant {TenantId}")]
    private static partial void LogAgentRegistered(ILogger logger, AgentId agentId, string externalId, TenantId tenantId);

    public async Task<Shared.Domain.Result<AgentId>> Handle(RegisterAgentCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return Result.Failure<AgentId>(Error.Unauthorized(
                "Tenant.Required",
                "Tenant context is required to register an agent."));
        }

        var tenantId = new TenantId(Guid.Parse(requestContext.TenantId));

        var exists = await agentRepository.ExistsAsync(tenantId, request.ExternalId, ct).ConfigureAwait(false);
        if (exists)
        {
            return Result.Failure<AgentId>(Error.Conflict(
                "Agent.AlreadyExists",
                $"An agent with external ID '{request.ExternalId}' already exists in this tenant."));
        }

        var createResult = Agent.Create(
            tenantId,
            request.ExternalId,
            request.Name,
            request.Framework,
            request.ModelProvider,
            request.ModelName,
            request.Manifest,
            requestContext.UserId ?? Guid.Empty,
            dateTimeProvider);

        if (createResult.IsFailure)
        {
            return Result.Failure<AgentId>(createResult.Error!);
        }

        var agent = createResult.Value!;

        await agentRepository.AddAsync(agent, ct).ConfigureAwait(false);

        foreach (var domainEvent in agent.DomainEvents)
        {
            await eventBus.PublishAsync(domainEvent, ct).ConfigureAwait(false);
        }
        agent.ClearDomainEvents();

        LogAgentRegistered(logger, agent.Id, agent.ExternalId, tenantId);

        return Result.Success(agent.Id);
    }
}
