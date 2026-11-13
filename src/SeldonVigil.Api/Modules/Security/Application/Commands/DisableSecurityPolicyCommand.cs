using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

// Only ICommand, not ICommand<Unit>. The MediatR.Unit in ICommand<Unit> made this
// command implement two different IRequest contracts; MediatR then could not match
// either to the handler, which implements the app's Unit via ICommandHandler<T>.
internal sealed record DisableSecurityPolicyCommand(
    SecurityPolicyId Id) : ICommand;
