using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed class GetSecurityPolicyByIdQueryHandler(
    ISecurityPolicyRepository policyRepository)
    : IQueryHandler<GetSecurityPolicyByIdQuery, SecurityPolicyDto>
{
    public async Task<Shared.Domain.Result<SecurityPolicyDto>> Handle(
        GetSecurityPolicyByIdQuery request, CancellationToken ct)
    {
        var policy = await policyRepository.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (policy is null)
        {
            return Shared.Domain.Result<SecurityPolicyDto>.Failure(Error.NotFound(
                "Policy.NotFound", "Security policy not found."));
        }

        var result = new SecurityPolicyDto(
            policy.Id.Value,
            policy.Name,
            policy.Description,
            policy.Category.ToString(),
            policy.RuleDefinition,
            policy.Severity.ToString(),
            policy.AutoResponse?.ToString(),
            policy.Status.ToString(),
            policy.CreatedAt,
            policy.UpdatedAt);

        return Result.Success(result);
    }
}
