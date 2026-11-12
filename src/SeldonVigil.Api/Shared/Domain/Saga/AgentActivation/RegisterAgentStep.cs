using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Shared.Domain.Saga.AgentActivation;

internal sealed class RegisterAgentStep(
    ILogger<RegisterAgentStep> logger,
    IAgentRepository agentRepository,
    IDateTimeProvider dateTimeProvider) : SagaStep<AgentActivationSagaData>
{
    public override string Name => "RegisterAgent";
    public override int Order => 1;

    public override async Task ExecuteAsync(AgentActivationSagaData sagaData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Registering agent {ExternalId} in Identity module", sagaData.ExternalId);

        var tenantId = new TenantId(sagaData.TenantId);

        var exists = await agentRepository.ExistsAsync(tenantId, sagaData.ExternalId, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            logger.LogWarning("Agent with external ID {ExternalId} already exists in tenant {TenantId}", sagaData.ExternalId, sagaData.TenantId);
            var existing = await agentRepository.GetByExternalIdAsync(tenantId, sagaData.ExternalId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                sagaData.AgentId = existing.Id.Value;
                sagaData.AgentRegistered = true;
                return;
            }
        }

        if (!Enum.TryParse<AgentFramework>(sagaData.Framework, ignoreCase: true, out var framework))
        {
            framework = AgentFramework.Custom;
        }

        if (!Enum.TryParse<ModelProvider>(sagaData.ModelProvider, ignoreCase: true, out var modelProvider))
        {
            modelProvider = ModelProvider.Custom;
        }

        var manifest = new CapabilityManifest();
        var createResult = Agent.Create(
            tenantId,
            sagaData.ExternalId,
            sagaData.Name,
            framework,
            modelProvider,
            sagaData.ModelName,
            manifest,
            sagaData.UserId,
            dateTimeProvider);

        if (createResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create agent: {createResult.Error?.Message}");
        }

        var agent = createResult.Value!;
        await agentRepository.AddAsync(agent, cancellationToken).ConfigureAwait(false);

        sagaData.AgentId = agent.Id.Value;
        sagaData.AgentRegistered = true;

        logger.LogInformation("Agent {AgentId} registered successfully", sagaData.AgentId);
    }

    public override async Task CompensateAsync(AgentActivationSagaData sagaData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Compensating agent registration for {AgentId}", sagaData.AgentId);

        if (sagaData.AgentId != Guid.Empty)
        {
            var agentId = new AgentId(sagaData.AgentId);
            await agentRepository.DeleteAsync(agentId, cancellationToken).ConfigureAwait(false);
        }

        sagaData.AgentRegistered = false;

        logger.LogInformation("Agent {AgentId} registration compensated", sagaData.AgentId);
    }
}
