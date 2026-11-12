using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record CreateSecurityPolicyRequest(
    string Name,
    string Description,
    PolicyCategory Category,
    RuleDefinition RuleDefinition,
    Severity Severity,
    AutoResponse? AutoResponse);
