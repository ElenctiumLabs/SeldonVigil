using FluentValidation;
using SeldonVigil.Api.Modules.Identity.Application.Commands;

namespace SeldonVigil.Api.Modules.Identity.Application.Validators;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant context is required.")
            .Must(BeValidGuid)
            .WithMessage("Tenant ID must be a valid GUID.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }

    private static bool BeValidGuid(string? value) =>
        string.IsNullOrWhiteSpace(value) || Guid.TryParse(value, out _);
}
