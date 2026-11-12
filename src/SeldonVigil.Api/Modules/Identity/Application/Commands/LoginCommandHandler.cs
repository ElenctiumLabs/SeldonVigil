using Microsoft.Extensions.Options;
using SeldonVigil.Api.Modules.Identity.Api.Endpoints;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Infrastructure;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed partial class LoginCommandHandler(
    IUserRepository userRepository,
    IUserSessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<DevAuthOptions> devAuthOptions,
    IHostEnvironment environment,
    IDateTimeProvider dateTimeProvider,
    ILogger<LoginCommandHandler> logger)
    : ICommandHandler<LoginCommand, LoginResult>
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Using default tenant ID for development login")]
    private static partial void LogDefaultTenantUsed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dev-mode login used for {Username} in tenant {TenantId}")]
    private static partial void LogDevModeLogin(ILogger logger, string username, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login attempt for unknown user {Username} in tenant {TenantId}")]
    private static partial void LogUnknownUserLogin(ILogger logger, string username, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed login for user {UserId}")]
    private static partial void LogFailedLogin(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} logged in")]
    private static partial void LogUserLoggedIn(ILogger logger, Guid userId);

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        // For development, use a default tenant ID if none is provided
        Guid tenantGuid;
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            if (environment.IsDevelopment())
            {
                tenantGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
                LogDefaultTenantUsed(logger);
            }
            else
            {
                return Result<LoginResult>.Failure(Error.Unauthorized(
                    "Tenant.Required",
                    "Tenant context is required to authenticate."));
            }
        }
        else
        {
            tenantGuid = Guid.Parse(request.TenantId);
        }

        var tenantId = new TenantId(tenantGuid);

        var user = await userRepository.GetByEmailAsync(tenantId, request.Username, ct).ConfigureAwait(false);
        if (user is null)
        {
            if (TryDevLogin(request))
            {
                LogDevModeLogin(logger, request.Username, tenantId.ToString());
                return Result<LoginResult>.Success(await IssueDevTokens(
                    tenantId, request.Username, request.IpAddress, request.UserAgent, ct).ConfigureAwait(false));
            }

            LogUnknownUserLogin(logger, request.Username, tenantId.ToString());
            return Result<LoginResult>.Failure(Error.Unauthorized(
                "Auth.InvalidCredentials",
                "Invalid username or password."));
        }

        if (user.IsLocked(dateTimeProvider))
        {
            return Result<LoginResult>.Failure(Error.Unauthorized(
                "Auth.AccountLocked",
                "Account is temporarily locked due to too many failed attempts."));
        }

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Dev backdoor: any known/unknown username with password 'dev' issues dev
            // tokens when DevAuth is enabled - seeded hashes may not be verifiable.
            if (TryDevLogin(request))
            {
                LogDevModeLogin(logger, request.Username, tenantId.ToString());
                return Result<LoginResult>.Success(await IssueDevTokens(
                    tenantId, request.Username, request.IpAddress, request.UserAgent, ct).ConfigureAwait(false));
            }

            user.RecordLoginAttempt(false, dateTimeProvider);
            await userRepository.UpdateAsync(user, ct).ConfigureAwait(false);
            LogFailedLogin(logger, user.Id);
            return Result<LoginResult>.Failure(Error.Unauthorized(
                "Auth.InvalidCredentials",
                "Invalid username or password."));
        }

        user.RecordLoginAttempt(true, dateTimeProvider);
        await userRepository.UpdateAsync(user, ct).ConfigureAwait(false);

        var tokens = tokenService.Issue(user, tenantId, request.IpAddress, request.UserAgent);

        var session = UserSession.Create(
            user.Id,
            tenantId,
            tokenService.HashToken(tokens.AccessToken),
            tokenService.HashToken(tokens.RefreshToken),
            tokens.RefreshExpiresAt,
            request.IpAddress,
            request.UserAgent,
            deviceFingerprint: null,
            dateTimeProvider.UtcNow);

        await sessionRepository.AddAsync(session, ct).ConfigureAwait(false);

        LogUserLoggedIn(logger, user.Id);

        return Result<LoginResult>.Success(new LoginResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            user.Name,
            [MapRole(user.Role)],
            tokens.AccessExpiresAt));
    }

    private static string MapRole(Domain.Enums.UserRole role) =>
        role.ToString().ToLowerInvariant();

    private bool TryDevLogin(LoginCommand request)
    {
        var options = devAuthOptions.Value;
        return options.Enabled &&
               environment.IsDevelopment() &&
               !string.IsNullOrWhiteSpace(request.Username) &&
               request.Password == "dev";
    }

    private async Task<LoginResult> IssueDevTokens(
        TenantId tenantId,
        string username,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var devUser = User.Create(
            tenantId,
            username,
            username,
            Domain.Enums.UserRole.Admin,
            passwordHash: null,
            dateTimeProvider).Value!;

        var tokens = tokenService.Issue(devUser, tenantId, ipAddress, userAgent);

        // Skip DB session persistence in dev mode — the dev user has no real DB row,
        // so inserting a UserSession would violate the FK constraint on users.
        // The JWT itself carries all the claims needed; no server-side session required.
        try
        {
            var session = UserSession.Create(
                devUser.Id,
                tenantId,
                tokenService.HashToken(tokens.AccessToken),
                tokenService.HashToken(tokens.RefreshToken),
                tokens.RefreshExpiresAt,
                ipAddress,
                userAgent,
                deviceFingerprint: null,
                dateTimeProvider.UtcNow);

            await sessionRepository.AddAsync(session, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Dev login: skipping session persistence (no real user row in DB)");
        }

        return new LoginResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            username,
            ["admin", "engineer", "security_analyst"],
            tokens.AccessExpiresAt);
    }
}
