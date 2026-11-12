using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Infrastructure;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed partial class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    ILogger<RegisterUserCommandHandler> logger)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} registered for tenant {TenantId}")]
    private static partial void LogUserRegistered(ILogger logger, Guid userId, TenantId tenantId);

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return Result<Guid>.Failure(Error.Unauthorized(
                "Tenant.Required",
                "Tenant context is required to register a user."));
        }

        var tenantId = new TenantId(Guid.Parse(request.TenantId));

        var exists = await userRepository.ExistsAsync(tenantId, request.Email, ct).ConfigureAwait(false);
        if (exists)
        {
            return Result<Guid>.Failure(Error.Conflict(
                "User.AlreadyExists",
                "A user with this email already exists in the tenant."));
        }

        if (!TryParseRole(request.Role, out var role))
        {
            return Result<Guid>.Failure(Error.Validation(
                "User.InvalidRole",
                $"'{request.Role}' is not a valid role."));
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        var createResult = User.Create(
            tenantId,
            request.Email,
            request.Name,
            role,
            passwordHash,
            dateTimeProvider);

        if (createResult.IsFailure)
        {
            return Result<Guid>.Failure(createResult.Error!);
        }

        var user = createResult.Value!;
        await userRepository.AddAsync(user, ct).ConfigureAwait(false);

        LogUserRegistered(logger, user.Id, tenantId);
        return Result<Guid>.Success(user.Id);
    }

    private static bool TryParseRole(string role, out UserRole result)
    {
        switch (role.ToLowerInvariant())
        {
            case "super_admin":
                result = UserRole.SuperAdmin;
                return true;
            case "admin":
                result = UserRole.Admin;
                return true;
            case "engineer":
                result = UserRole.Engineer;
                return true;
            case "security_analyst":
                result = UserRole.SecurityAnalyst;
                return true;
            case "viewer":
                result = UserRole.Viewer;
                return true;
            case "billing":
                result = UserRole.Billing;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
