using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Domain.Entities;

internal sealed class User : BaseAggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string? SsoSubject { get; private set; }
    public string? SsoProvider { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? MfaSecretEncrypted { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int LoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    private User() { }

    public static Result<User> Create(
        TenantId tenantId,
        string email,
        string name,
        UserRole role,
        string? passwordHash,
        IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<User>.Failure(Error.Validation("User.InvalidEmail", "Email is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<User>.Failure(Error.Validation("User.InvalidName", "Name is required."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Role = role,
            PasswordHash = passwordHash,
            CreatedAt = dateTimeProvider.UtcNow,
            UpdatedAt = dateTimeProvider.UtcNow
        };

        return Result<User>.Success(user);
    }

    public Result<Unit> RecordLoginAttempt(bool success, IDateTimeProvider dateTimeProvider)
    {
        if (success)
        {
            LoginAttempts = 0;
            LockedUntil = null;
            LastLoginAt = dateTimeProvider.UtcNow;
            UpdatedAt = dateTimeProvider.UtcNow;
            return Result<Unit>.Success(Unit.Value);
        }

        LoginAttempts++;
        if (LoginAttempts >= 5)
        {
            LockedUntil = dateTimeProvider.UtcNow.AddMinutes(15);
            LoginAttempts = 0;
        }
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result<Unit>.Success(Unit.Value);
    }

    public bool IsLocked(IDateTimeProvider dateTimeProvider) =>
        LockedUntil.HasValue && LockedUntil > dateTimeProvider.UtcNow;

    public Result<Unit> UpdatePassword(string newPasswordHash, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return Result<Unit>.Failure(Error.Validation("User.InvalidPassword", "Password hash is required."));
        }

        PasswordHash = newPasswordHash;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result<Unit>.Success(Unit.Value);
    }
}
