using FluentValidation;
using SeldonVigil.Api.Modules.Identity.Application.Commands;

namespace SeldonVigil.Api.Modules.Identity.Application.Validators;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    private readonly IHostEnvironment _environment;

    public LoginCommandValidator(IHostEnvironment environment)
    {
        _environment = environment;

        // Skip all validation in development mode
        if (_environment.IsDevelopment())
        {
            return;
        }

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant context is required to authenticate.")
            .Must(BeValidGuid)
            .WithMessage("Tenant ID must be a valid GUID.");

        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(255)
            .EmailAddress()
            .WithMessage("Username must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }

    private static bool BeValidGuid(string? value) =>
        string.IsNullOrWhiteSpace(value) || Guid.TryParse(value, out _);
}
