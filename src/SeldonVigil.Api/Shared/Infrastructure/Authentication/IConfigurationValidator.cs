namespace SeldonVigil.Api.Shared.Infrastructure.Authentication;

public interface IConfigurationValidator
{
    ValidationResult Validate(IConfiguration configuration);

    /// <summary>Validates against an explicit environment name (authoritative over config-derived values).</summary>
    ValidationResult Validate(IConfiguration configuration, string? environmentName);
}

public record ValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ValidationResult Success => new ValidationResult(true);
}

