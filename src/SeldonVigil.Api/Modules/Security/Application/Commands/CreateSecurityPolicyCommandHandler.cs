using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed partial class CreateSecurityPolicyCommandHandler(
    ISecurityPolicyRepository policyRepository,
    IDateTimeProvider dateTimeProvider,
    IRequestContext requestContext,
    ILogger<CreateSecurityPolicyCommandHandler> logger)
    : ICommandHandler<CreateSecurityPolicyCommand, SecurityPolicyId>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Security policy created: {PolicyId} ({PolicyName}) for tenant {TenantId}")]
    private static partial void LogSecurityPolicyCreated(ILogger logger, SecurityPolicyId policyId, string policyName, TenantId tenantId);

    public async Task<Shared.Domain.Result<SecurityPolicyId>> Handle(
        CreateSecurityPolicyCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return Shared.Domain.Result<SecurityPolicyId>.Failure(Error.Unauthorized(
                "Tenant.Required", "Tenant context is required."));
        }

        var tenantId = new TenantId(Guid.Parse(requestContext.TenantId));

        var policyResult = SecurityPolicy.Create(
            tenantId,
            request.Name,
            request.Description,
            request.Category,
            request.RuleDefinition,
            request.Severity,
            request.AutoResponse,
            requestContext.UserId ?? Guid.Empty,
            dateTimeProvider);

        if (policyResult.IsFailure)
        {
            return Shared.Domain.Result<SecurityPolicyId>.Failure(policyResult.Error!);
        }

        var policy = policyResult.Value!;
        await policyRepository.AddAsync(policy, ct).ConfigureAwait(false);

        LogSecurityPolicyCreated(logger, policy.Id, policy.Name, tenantId);

        return Result.Success(policy.Id);
    }
}
