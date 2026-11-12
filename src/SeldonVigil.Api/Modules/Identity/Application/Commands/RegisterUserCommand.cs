using SeldonVigil.SharedKernel.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed record RegisterUserCommand(
    string TenantId,
    string Email,
    string Name,
    string Password,
    string Role) : ICommand<Guid>;
