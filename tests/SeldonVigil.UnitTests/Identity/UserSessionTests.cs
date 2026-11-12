using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Identity.Domain.Entities;

public class UserSessionTests
{
    [Fact]
    public void CreateWithValidParametersReturnsSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var tokenHash = "token-hash";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var createdAt = DateTime.UtcNow;

        // Act
        var session = UserSession.Create(
            userId,
            tenantId,
            tokenHash,
            "refresh-token-hash",
            expiresAt,
            "127.0.0.1",
            "Mozilla/5.0",
            "device-fingerprint",
            createdAt);

        // Assert
        _ = session.UserId.Should().Be(userId);
        _ = session.TenantId.Should().Be(tenantId);
        _ = session.TokenHash.Should().Be(tokenHash);
        _ = session.RefreshTokenHash.Should().Be("refresh-token-hash");
        _ = session.ExpiresAt.Should().Be(expiresAt);
        _ = session.IpAddress.Should().Be("127.0.0.1");
        _ = session.UserAgent.Should().Be("Mozilla/5.0");
        _ = session.DeviceFingerprint.Should().Be("device-fingerprint");
        _ = session.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void CreateWithMinimalParametersReturnsSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var tokenHash = "token-hash";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var createdAt = DateTime.UtcNow;

        // Act
        var session = UserSession.Create(
            userId,
            tenantId,
            tokenHash,
            null,
            expiresAt,
            null,
            null,
            null,
            createdAt);

        // Assert
        _ = session.UserId.Should().Be(userId);
        _ = session.TokenHash.Should().Be(tokenHash);
        _ = session.RefreshTokenHash.Should().BeNull();
        _ = session.IpAddress.Should().BeNull();
        _ = session.UserAgent.Should().BeNull();
        _ = session.DeviceFingerprint.Should().BeNull();
    }

    [Fact]
    public void RevokeSetsRevokedAtAndReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var session = UserSession.Create(
            userId,
            tenantId,
            "token-hash",
            null,
            DateTime.UtcNow.AddHours(1),
            null,
            null,
            null,
            DateTime.UtcNow);
        var revokedAt = DateTime.UtcNow;
        var reason = "User logged out";

        // Act
        session.Revoke(reason, revokedAt);

        // Assert
        _ = session.RevokedAt.Should().Be(revokedAt);
        _ = session.RevokedReason.Should().Be(reason);
    }

    [Fact]
    public void IsActiveWhenNotRevokedAndNotExpiredReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var session = UserSession.Create(
            userId,
            tenantId,
            "token-hash",
            null,
            DateTime.UtcNow.AddHours(1),
            null,
            null,
            null,
            DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // Act
        var result = session.IsActive(now);

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public void IsActiveWhenRevokedReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var session = UserSession.Create(
            userId,
            tenantId,
            "token-hash",
            null,
            DateTime.UtcNow.AddHours(1),
            null,
            null,
            null,
            DateTime.UtcNow);
        session.Revoke("User logged out", DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // Act
        var result = session.IsActive(now);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void IsActiveWhenExpiredReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var session = UserSession.Create(
            userId,
            tenantId,
            "token-hash",
            null,
            DateTime.UtcNow.AddHours(-1),
            null,
            null,
            null,
            DateTime.UtcNow.AddHours(-2));
        var now = DateTime.UtcNow;

        // Act
        var result = session.IsActive(now);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void IsActiveWhenRevokedAndExpiredReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var session = UserSession.Create(
            userId,
            tenantId,
            "token-hash",
            null,
            DateTime.UtcNow.AddHours(-1),
            null,
            null,
            null,
            DateTime.UtcNow.AddHours(-2));
        session.Revoke("User logged out", DateTime.UtcNow.AddHours(-1.5));
        var now = DateTime.UtcNow;

        // Act
        var result = session.IsActive(now);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void CreateGeneratesNewId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = TenantId.New();
        var createdAt = DateTime.UtcNow;

        // Act
        var session1 = UserSession.Create(
            userId,
            tenantId,
            "token-hash",
            null,
            DateTime.UtcNow.AddHours(1),
            null,
            null,
            null,
            createdAt);
        var session2 = UserSession.Create(
            userId,
            tenantId,
            "token-hash",
            null,
            DateTime.UtcNow.AddHours(1),
            null,
            null,
            null,
            createdAt);

        // Assert
        _ = session1.Id.Should().NotBe(session2.Id);
    }
}
