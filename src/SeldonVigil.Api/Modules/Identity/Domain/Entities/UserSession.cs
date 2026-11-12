using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Domain.Entities;

internal sealed class UserSession : SeldonVigil.SharedKernel.Domain.BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public TenantId TenantId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? RefreshTokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceFingerprint { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    private UserSession() { }

    public static UserSession Create(
        Guid userId,
        TenantId tenantId,
        string tokenHash,
        string? refreshTokenHash,
        DateTime expiresAt,
        string? ipAddress,
        string? userAgent,
        string? deviceFingerprint,
        DateTime createdAt)
    {
        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            TokenHash = tokenHash,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Revoke(string reason, DateTime revokedAt)
    {
        RevokedAt = revokedAt;
        RevokedReason = reason;
        UpdatedAt = revokedAt;
    }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
