using SeldonVigil.SharedKernel.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed record LogoutCommand(string RefreshToken, Guid? UserId) : ICommand;
