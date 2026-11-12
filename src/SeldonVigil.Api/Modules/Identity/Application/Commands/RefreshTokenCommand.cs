using SeldonVigil.SharedKernel.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed record RefreshTokenCommand(
    string TenantId,
    string RefreshToken,
    string? IpAddress,
    string? UserAgent) : ICommand<LoginResult>;
