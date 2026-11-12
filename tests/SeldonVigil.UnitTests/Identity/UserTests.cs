using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Identity.Domain.Entities;

public class UserTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public UserTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void CreateWithValidParametersReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();

        // Act
        var result = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Email.Should().Be("test@example.com");
        _ = result.Value.Name.Should().Be("Test User");
        _ = result.Value.Role.Should().Be(UserRole.Admin);
        _ = result.Value.PasswordHash.Should().Be("password-hash");
        _ = result.Value.LoginAttempts.Should().Be(0);
    }

    [Fact]
    public void CreateWithInvalidEmailReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();

        // Act
        var result = User.Create(
            tenantId,
            "", // Invalid: empty
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("User.InvalidEmail");
    }

    [Fact]
    public void CreateWithInvalidNameReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();

        // Act
        var result = User.Create(
            tenantId,
            "test@example.com",
            "", // Invalid: empty
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("User.InvalidName");
    }

    [Fact]
    public void CreateNormalizesEmailToLowercase()
    {
        // Arrange
        var tenantId = TenantId.New();

        // Act
        var result = User.Create(
            tenantId,
            "Test@Example.COM",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.Value!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void RecordLoginAttemptWithSuccessResetsAttempts()
    {
        // Arrange
        var tenantId = TenantId.New();
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object).Value!;
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);

        // Act
        var result = user.RecordLoginAttempt(true, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = user.LoginAttempts.Should().Be(0);
        _ = user.LockedUntil.Should().BeNull();
        _ = user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordLoginAttemptWithFailureIncreasesAttempts()
    {
        // Arrange
        var tenantId = TenantId.New();
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object).Value!;

        // Act
        var result = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = user.LoginAttempts.Should().Be(1);
    }

    [Fact]
    public void RecordLoginAttemptWithFiveFailuresLocksAccount()
    {
        // Arrange
        var tenantId = TenantId.New();
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object).Value!;

        // Act
        for (int i = 0; i < 5; i++)
        {
            _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        }

        // Assert
        _ = user.LoginAttempts.Should().Be(0); // Reset after lock
        _ = user.LockedUntil.Should().NotBeNull();
        _ = user.LockedUntil.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void IsLockedWhenLockedUntilInFutureReturnsTrue()
    {
        // Arrange
        var tenantId = TenantId.New();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object).Value!;
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow.AddMinutes(5));

        // Act
        var result = user.IsLocked(_dateTimeProviderMock.Object);

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public void IsLockedWhenLockedUntilInPastReturnsFalse()
    {
        // Arrange
        var tenantId = TenantId.New();
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object).Value!;
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = user.RecordLoginAttempt(false, _dateTimeProviderMock.Object);
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow.AddMinutes(20));

        // Act
        var result = user.IsLocked(_dateTimeProviderMock.Object);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedWhenNotLockedReturnsFalse()
    {
        // Arrange
        var tenantId = TenantId.New();
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "password-hash",
            _dateTimeProviderMock.Object).Value!;

        // Act
        var result = user.IsLocked(_dateTimeProviderMock.Object);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void UpdatePasswordWithValidHashUpdatesPassword()
    {
        // Arrange
        var tenantId = TenantId.New();
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "old-password-hash",
            _dateTimeProviderMock.Object).Value!;
        var newPasswordHash = "new-password-hash";

        // Act
        var result = user.UpdatePassword(newPasswordHash, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = user.PasswordHash.Should().Be(newPasswordHash);
    }

    [Fact]
    public void UpdatePasswordWithInvalidHashReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var user = User.Create(
            tenantId,
            "test@example.com",
            "Test User",
            UserRole.Admin,
            "old-password-hash",
            _dateTimeProviderMock.Object).Value!;

        // Act
        var result = user.UpdatePassword("", _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("User.InvalidPassword");
    }
}
