using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record UpdateSecurityPolicyRequest(
    string Name,
    string Description,
    RuleDefinition RuleDefinition,
    Severity Severity,
    AutoResponse? AutoResponse);
