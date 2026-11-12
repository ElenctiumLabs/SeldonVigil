namespace SeldonVigil.Api.Shared.Infrastructure.Authentication;

public class ConfigurationValidator : IConfigurationValidator
{
    public ValidationResult Validate(IConfiguration configuration) =>
        Validate(configuration,
            configuration["ASPNETCORE_ENVIRONMENT"] ??
            configuration["DOTNET_ENVIRONMENT"]);

    public ValidationResult Validate(IConfiguration configuration, string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = new List<string>();

        // Validate authentication configuration
        var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
        var devAuthEnabled = configuration.GetValue("DevAuth:Enabled", false);

        // In development with DevAuth enabled, validate DevAuth settings instead
        if (isDevelopment && devAuthEnabled)
        {
            var devAuthSecretKey = configuration["DevAuth:SecretKey"];
            if (string.IsNullOrWhiteSpace(devAuthSecretKey))
            {
                errors.Add("DevAuth:SecretKey must be configured in appsettings for development authentication.");
            }

            var devAuthIssuer = configuration["DevAuth:Issuer"];
            if (string.IsNullOrWhiteSpace(devAuthIssuer))
            {
                errors.Add("DevAuth:Issuer must be configured in appsettings for development authentication.");
            }

            var devAuthAudience = configuration["DevAuth:Audience"];
            if (string.IsNullOrWhiteSpace(devAuthAudience))
            {
                errors.Add("DevAuth:Audience must be configured in appsettings for development authentication.");
            }
        }
        else if (!isDevelopment)
        {
            // Validate OIDC authentication configuration for production/staging only
            var authority = configuration["Authentication:Authority"];
            if (string.IsNullOrWhiteSpace(authority))
            {
                errors.Add("Authentication:Authority must be configured in appsettings. For development, set Authentication:Authority to your OIDC provider URL. This setting is required in all environments.");
            }

            var audience = configuration["Authentication:Audience"];
            if (string.IsNullOrWhiteSpace(audience))
            {
                errors.Add("Authentication:Audience must be configured in appsettings.");
            }
        }

        // Azure KeyVault is optional: when Azure:KeyVaultName is set the API loads
        // secrets from it, otherwise secrets come from environment variables.

        // Validate connection strings
        var postgresConnectionString = configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            errors.Add("ConnectionStrings:PostgreSQL must be configured in appsettings.");
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            errors.Add("ConnectionStrings:Redis must be configured in appsettings.");
        }

        var brokerConnectionString = configuration.GetConnectionString("Broker");
        if (string.IsNullOrWhiteSpace(brokerConnectionString))
        {
            errors.Add("ConnectionStrings:Broker must be configured in appsettings.");
        }

        // Production brokers are hardened; require the TLS/SASL client keys
        // (SCRAM principals created during provisioning).
        if (!isDevelopment)
        {
            var brokerSecurityProtocol = configuration["Broker:SecurityProtocol"];
            if (string.IsNullOrWhiteSpace(brokerSecurityProtocol))
            {
                errors.Add("Broker:SecurityProtocol must be configured in production (expected SASL_SSL).");
            }

            var brokerSaslMechanism = configuration["Broker:SaslMechanism"];
            if (string.IsNullOrWhiteSpace(brokerSaslMechanism))
            {
                errors.Add("Broker:SaslMechanism must be configured in production (e.g. SCRAM-SHA-256).");
            }

            if (string.IsNullOrWhiteSpace(configuration["Broker:SaslUsername"]))
            {
                errors.Add("Broker:SaslUsername must be configured in production.");
            }

            if (string.IsNullOrWhiteSpace(configuration["Broker:SaslPassword"]))
            {
                errors.Add("Broker:SaslPassword must be configured in production.");
            }

            if (string.IsNullOrWhiteSpace(configuration["Broker:SslCaLocation"]))
            {
                errors.Add("Broker:SslCaLocation must be configured in production.");
            }
        }

        if (errors.Count > 0)
        {
            return new ValidationResult(false, string.Join(" | ", errors));
        }

        return ValidationResult.Success;
    }
}

// public static class ValidationResult
// {
//     public static readonly ValidationResult Success = new(true, null);
// }
