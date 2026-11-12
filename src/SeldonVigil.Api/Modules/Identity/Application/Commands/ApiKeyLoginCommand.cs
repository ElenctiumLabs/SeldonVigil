using System.Security.Cryptography;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Infrastructure;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed record ServiceApiKeyEntry(string Key, Guid TenantId, string[] Roles);

internal sealed record ApiKeyLoginCommand(
    string ApiKey,
    string? IpAddress,
    string? UserAgent) : ICommand<ApiKeyLoginResult>;

internal sealed record ApiKeyLoginResult(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresInSeconds);

internal sealed partial class ApiKeyLoginCommandHandler(
    IUserSessionRepository sessionRepository,
    ITokenService tokenService,
    IConfiguration configuration,
    IDateTimeProvider dateTimeProvider,
    ILogger<ApiKeyLoginCommandHandler> logger)
    : ICommandHandler<ApiKeyLoginCommand, ApiKeyLoginResult>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "API key login succeeded for tenant {TenantId}")]
    private static partial void LogApiKeyLoginSucceeded(ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected API key login attempt from {IpAddress}")]
    private static partial void LogApiKeyLoginRejected(ILogger logger, string? ipAddress);

    public async Task<Result<ApiKeyLoginResult>> Handle(ApiKeyLoginCommand request, CancellationToken ct)
    {
        var entries = configuration
            .GetSection("Identity:ServiceApiKeys")
            .Get<List<ServiceApiKeyEntry>>() ?? [];

        // Constant-time comparison per entry to avoid leaking key prefixes on match length.
        var entry = entries.FirstOrDefault(e =>
            !string.IsNullOrWhiteSpace(e.Key) &&
            CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(request.ApiKey.PadRight(128)),
                System.Text.Encoding.UTF8.GetBytes(e.Key.PadRight(128))));

        if (entry is null)
        {
            LogApiKeyLoginRejected(logger, request.IpAddress);
            return Result<ApiKeyLoginResult>.Failure(Error.Unauthorized(
                "Auth.InvalidApiKey",
                "Invalid API key."));
        }

        var tenantId = new TenantId(entry.TenantId);
        var role = ParseRole(entry.Roles);

        // Synthetic service identity - service keys authenticate machines, not users.
        var serviceUser = User.Create(
            tenantId,
            $"svc-{entry.TenantId:N}",
            "Service Account",
            role,
            passwordHash: null,
            dateTimeProvider).Value!;

        var tokens = tokenService.Issue(serviceUser, tenantId, request.IpAddress, request.UserAgent);

        var session = UserSession.Create(
            serviceUser.Id,
            tenantId,
            tokenService.HashToken(tokens.AccessToken),
            tokenService.HashToken(tokens.RefreshToken),
            tokens.RefreshExpiresAt,
            request.IpAddress,
            request.UserAgent,
            deviceFingerprint: null,
            dateTimeProvider.UtcNow);

        await sessionRepository.AddAsync(session, ct).ConfigureAwait(false);

        LogApiKeyLoginSucceeded(logger, entry.TenantId);

        var expiresIn = (int)TimeSpan.FromMinutes(15).TotalSeconds;
        return Result<ApiKeyLoginResult>.Success(new ApiKeyLoginResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            "Bearer",
            Math.Max(1, expiresIn)));
    }

    private static UserRole ParseRole(string[] roles)
    {
        foreach (var role in roles)
        {
            if (Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
            {
                return parsed;
            }
        }

        // Least privilege: an API key with missing/unparsable roles must never
        // escalate to Admin. Viewer is the safe floor.
        return UserRole.Viewer;
    }
}
