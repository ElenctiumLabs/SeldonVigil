using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Infrastructure;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed partial class RefreshTokenCommandHandler(
    IUserSessionRepository sessionRepository,
    IUserRepository userRepository,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider,
    ILogger<RefreshTokenCommandHandler> logger)
    : ICommandHandler<RefreshTokenCommand, LoginResult>
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token reuse detected for user {UserId}")]
    private static partial void LogRefreshTokenReuse(ILogger logger, Guid userId);

    public async Task<Result<LoginResult>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return Result<LoginResult>.Failure(Error.Unauthorized(
                "Tenant.Required",
                "Tenant context is required."));
        }

        var tenantId = new TenantId(Guid.Parse(request.TenantId));
        var refreshTokenHash = tokenService.HashToken(request.RefreshToken);

        var session = await sessionRepository
            .GetByRefreshTokenHashAsync(refreshTokenHash, ct)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<LoginResult>.Failure(Error.Unauthorized(
                "Auth.InvalidRefreshToken",
                "Invalid refresh token."));
        }

        if (!session.IsActive(dateTimeProvider.UtcNow))
        {
            await sessionRepository.RevokeAllForUserAsync(
                session.UserId,
                "Refresh token reuse detected",
                dateTimeProvider.UtcNow,
                ct).ConfigureAwait(false);
            LogRefreshTokenReuse(logger, session.UserId);
            return Result<LoginResult>.Failure(Error.Unauthorized(
                "Auth.InvalidRefreshToken",
                "Invalid refresh token."));
        }

        var user = await userRepository.GetByIdAsync(session.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            return Result<LoginResult>.Failure(Error.Unauthorized(
                "Auth.InvalidRefreshToken",
                "Invalid refresh token."));
        }

        var tokens = tokenService.Issue(user, tenantId, request.IpAddress, request.UserAgent);

        session.Revoke("Rotated", dateTimeProvider.UtcNow);
        await sessionRepository.UpdateAsync(session, ct).ConfigureAwait(false);

        var newSession = UserSession.Create(
            user.Id,
            tenantId,
            tokenService.HashToken(tokens.AccessToken),
            tokenService.HashToken(tokens.RefreshToken),
            tokens.RefreshExpiresAt,
            request.IpAddress,
            request.UserAgent,
            deviceFingerprint: null,
            dateTimeProvider.UtcNow);

        await sessionRepository.AddAsync(newSession, ct).ConfigureAwait(false);

        return Result<LoginResult>.Success(new LoginResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            user.Name,
            [user.Role.ToString().ToLowerInvariant()],
            tokens.AccessExpiresAt));
    }
}
