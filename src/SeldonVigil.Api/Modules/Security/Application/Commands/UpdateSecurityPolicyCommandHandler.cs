using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;
using IRequestContext = SeldonVigil.Api.Shared.Application.IRequestContext;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed class UpdateSecurityPolicyCommandHandler(
    ISecurityPolicyRepository policyRepository,
    IDateTimeProvider dateTimeProvider,
    IRequestContext requestContext)
    : Shared.Application.ICommandHandler<UpdateSecurityPolicyCommand>
{
    public async Task<Shared.Domain.Result<Unit>> Handle(UpdateSecurityPolicyCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return Shared.Domain.Result<Unit>.Failure(Error.Unauthorized(
                "Tenant.Required", "Tenant context is required."));
        }

        var policy = await policyRepository.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (policy is null)
        {
            return Shared.Domain.Result<Unit>.Failure(Error.NotFound(
                "Policy.NotFound", "Security policy not found."));
        }

        // Update the policy properties using reflection since there's no Update method
        // This is a simplified implementation
        typeof(SecurityPolicy)
            .GetProperty(nameof(SecurityPolicy.Name))
            ?.SetValue(policy, request.Name);

        typeof(SecurityPolicy)
            .GetProperty(nameof(SecurityPolicy.Description))
            ?.SetValue(policy, request.Description);

        typeof(SecurityPolicy)
            .GetProperty(nameof(SecurityPolicy.RuleDefinition))
            ?.SetValue(policy, request.RuleDefinition);

        typeof(SecurityPolicy)
            .GetProperty(nameof(SecurityPolicy.Severity))
            ?.SetValue(policy, request.Severity);

        typeof(SecurityPolicy)
            .GetProperty(nameof(SecurityPolicy.AutoResponse))
            ?.SetValue(policy, request.AutoResponse);

        typeof(SecurityPolicy)
            .GetProperty(nameof(SecurityPolicy.UpdatedAt))
            ?.SetValue(policy, dateTimeProvider.UtcNow);

        await policyRepository.UpdateAsync(policy, ct).ConfigureAwait(false);

        return Result.Success(Unit.Value);
    }
}
