using MediatR;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed record DisableSecurityPolicyCommand(
    SecurityPolicyId Id) : ICommand<Unit>, ICommand;
