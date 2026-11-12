using SeldonVigil.SharedKernel.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed record LoginCommand(
    string TenantId,
    string Username,
    string Password,
    string? IpAddress,
    string? UserAgent) : ICommand<LoginResult>;

internal sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    string Username,
    string[] Roles,
    DateTime AccessExpiresAt);
