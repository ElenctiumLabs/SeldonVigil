using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.Shared.Domain.Saga;
using SeldonVigil.Shared.Domain.Saga.AgentActivation;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;
using IRequestContext = SeldonVigil.Api.Shared.Application.IRequestContext;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed partial class ActivateAgentCommandHandler(
    IAgentRepository agentRepository,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider,
    IRequestContext requestContext,
    ISagaOrchestrator sagaOrchestrator,
    IEnumerable<ISagaStep> sagaSteps,
    ILogger<ActivateAgentCommandHandler> logger)
    : Shared.Application.ICommandHandler<ActivateAgentCommand>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Agent activated: {AgentId}")]
    private static partial void LogAgentActivated(ILogger logger, AgentId agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saga completed for agent activation: {AgentId}")]
    private static partial void LogSagaCompleted(ILogger logger, AgentId agentId);

    public async Task<Shared.Domain.Result<Unit>> Handle(ActivateAgentCommand request, CancellationToken ct)
    {
        var agent = await agentRepository.GetByIdAsync(request.AgentId, ct).ConfigureAwait(false);
        if (agent is null)
        {
            return Shared.Domain.Result<Unit>.Failure(Error.NotFound(
                "Agent.NotFound",
                $"Agent with ID '{request.AgentId}' was not found."));
        }

        var result = agent.Activate(requestContext.UserId ?? Guid.Empty, dateTimeProvider);
        if (result.IsFailure)
        {
            return Shared.Domain.Result<Unit>.Failure(result.Error!);
        }

        await agentRepository.UpdateAsync(agent, ct).ConfigureAwait(false);

        foreach (var domainEvent in agent.DomainEvents)
        {
            await eventBus.PublishAsync(domainEvent, ct).ConfigureAwait(false);
        }
        agent.ClearDomainEvents();

        LogAgentActivated(logger, request.AgentId);

        // Execute post-activation saga: apply security policies and other
        // cross-module coordination. Failures here are logged but do not
        // block activation (the agent is already active).
        try
        {
            var activationSteps = sagaSteps
                .Where(s => s.Name == "ApplySecurityPolicies")
                .ToList();

            if (activationSteps.Count > 0)
            {
                var sagaData = new AgentActivationSagaData
                {
                    AgentId = request.AgentId.Value,
                    TenantId = Guid.TryParse(requestContext.TenantId, out var tid) ? tid : Guid.Empty,
                    UserId = requestContext.UserId ?? Guid.Empty,
                    SecurityPoliciesApplied = false,
                };

                await sagaOrchestrator.ExecuteAsync(
                    "AgentActivation",
                    request.AgentId.Value.ToString(),
                    activationSteps,
                    sagaData,
                    ct).ConfigureAwait(false);

                LogSagaCompleted(logger, request.AgentId);
            }
        }
        catch (Exception ex)
        {
            // Post-activation saga failure is non-fatal: the agent is already
            // active. Log and continue; the SagaRecoveryService will retry.
            logger.LogWarning(ex, "Post-activation saga failed for agent {AgentId}; will be retried", request.AgentId);
        }

        return Result.Success(Unit.Value);
    }
}
