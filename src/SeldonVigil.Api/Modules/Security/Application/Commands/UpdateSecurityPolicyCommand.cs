using MediatR;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed record UpdateSecurityPolicyCommand(
    SecurityPolicyId Id,
    string Name,
    string Description,
    RuleDefinition RuleDefinition,
    Severity Severity,
    AutoResponse? AutoResponse) : ICommand<Unit>, ICommand;
