using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;
using IRequestContext = SeldonVigil.Api.Shared.Application.IRequestContext;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed class DisableSecurityPolicyCommandHandler(
    ISecurityPolicyRepository policyRepository,
    IDateTimeProvider dateTimeProvider,
    IRequestContext requestContext)
    : Shared.Application.ICommandHandler<DisableSecurityPolicyCommand>
{
    public async Task<Shared.Domain.Result<Unit>> Handle(DisableSecurityPolicyCommand request, CancellationToken ct)
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

        policy.Disable(requestContext.UserId ?? Guid.Empty, dateTimeProvider);

        await policyRepository.UpdateAsync(policy, ct).ConfigureAwait(false);

        return Result.Success(Unit.Value);
    }
}
