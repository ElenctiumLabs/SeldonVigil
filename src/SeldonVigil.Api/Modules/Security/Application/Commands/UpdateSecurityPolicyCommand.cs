using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

// See DisableSecurityPolicyCommand: ICommand<Unit> here used MediatR.Unit and broke
// the handler resolution.
internal sealed record UpdateSecurityPolicyCommand(
    SecurityPolicyId Id,
    string Name,
    string Description,
    RuleDefinition RuleDefinition,
    Severity Severity,
    AutoResponse? AutoResponse) : ICommand;
