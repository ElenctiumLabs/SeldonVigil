using FluentValidation;
using SeldonVigil.Api.Modules.Identity.Application.Commands;

namespace SeldonVigil.Api.Modules.Identity.Application.Validators;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly string[] AllowedRoles =
    [
        "super_admin", "admin", "engineer", "security_analyst", "viewer", "billing"
    ];

    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant context is required to register a user.")
            .Must(BeValidGuid)
            .WithMessage("Tenant ID must be a valid GUID.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(255)
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .MinimumLength(2)
            .WithMessage("Name must be between 2 and 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .WithMessage("Password must be between 8 and 128 characters.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => AllowedRoles.Contains(role.ToLowerInvariant()))
            .WithMessage("Role must be one of: super_admin, admin, engineer, security_analyst, viewer, billing.");
    }

    private static bool BeValidGuid(string? value) =>
        string.IsNullOrWhiteSpace(value) || Guid.TryParse(value, out _);
}
