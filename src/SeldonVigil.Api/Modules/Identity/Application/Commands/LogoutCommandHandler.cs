using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Infrastructure;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed partial class LogoutCommandHandler(
    IUserSessionRepository sessionRepository,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider,
    ILogger<LogoutCommandHandler> logger)
    : ICommandHandler<LogoutCommand>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} logged out")]
    private static partial void LogUserLoggedOut(ILogger logger, Guid userId);

    public async Task<Result<Unit>> Handle(LogoutCommand request, CancellationToken ct)
    {
        var refreshTokenHash = tokenService.HashToken(request.RefreshToken);
        var session = await sessionRepository
            .GetByRefreshTokenHashAsync(refreshTokenHash, ct)
            .ConfigureAwait(false);

        if (session is not null)
        {
            session.Revoke("Logout", dateTimeProvider.UtcNow);
            await sessionRepository.UpdateAsync(session, ct).ConfigureAwait(false);
            LogUserLoggedOut(logger, session.UserId);
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
