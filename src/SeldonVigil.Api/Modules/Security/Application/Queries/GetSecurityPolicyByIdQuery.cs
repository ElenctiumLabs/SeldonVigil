using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed record GetSecurityPolicyByIdQuery(
    SecurityPolicyId Id) : IQuery<SecurityPolicyDto>;
