using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;

namespace SeldonVigil.Shared.Domain.Saga.AgentActivation;

internal sealed class ApplySecurityPoliciesStep(
    ILogger<ApplySecurityPoliciesStep> logger,
    ISecurityPolicyRepository securityPolicyRepository) : SagaStep<AgentActivationSagaData>
{
    public override string Name => "ApplySecurityPolicies";
    public override int Order => 2;

    public override async Task ExecuteAsync(AgentActivationSagaData sagaData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaData);

        logger.LogInformation("Applying security policies to agent {AgentId}", sagaData.AgentId);

        var tenantId = new TenantId(sagaData.TenantId);
        var policies = await securityPolicyRepository.GetActiveByTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (policies.Count == 0)
        {
            logger.LogWarning("No active security policies found for tenant {TenantId}", sagaData.TenantId);
        }
        else
        {
            logger.LogInformation("Found {PolicyCount} active policies for tenant {TenantId}. Policies are evaluated at runtime against agent traces.", policies.Count, sagaData.TenantId);
        }

        sagaData.SecurityPoliciesApplied = true;

        logger.LogInformation("Security policies applied to agent {AgentId}", sagaData.AgentId);
    }

    public override async Task CompensateAsync(AgentActivationSagaData sagaData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaData);

        logger.LogInformation("Compensating security policy application for agent {AgentId}", sagaData.AgentId);

        sagaData.SecurityPoliciesApplied = false;

        await Task.CompletedTask.ConfigureAwait(false);

        logger.LogInformation("Security policies compensated for agent {AgentId}", sagaData.AgentId);
    }
}
