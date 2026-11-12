using FluentValidation;
using SeldonVigil.Api.Modules.Identity.Application.Commands;

namespace SeldonVigil.Api.Modules.Identity.Application.Validators;

internal sealed class RegisterAgentCommandValidator : AbstractValidator<RegisterAgentCommand>
{
    public RegisterAgentCommandValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty()
            .MaximumLength(255)
            .Matches(@"^[\w\-]+$")
            .WithMessage("External ID must contain only letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .MinimumLength(2);

        RuleFor(x => x.Framework)
            .IsInEnum();

        RuleFor(x => x.ModelProvider)
            .IsInEnum();

        RuleFor(x => x.ModelName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Manifest)
            .NotNull()
            .Must(m => m.Tools.Count > 0)
            .WithMessage("Agent must declare at least one tool.");
    }
}
