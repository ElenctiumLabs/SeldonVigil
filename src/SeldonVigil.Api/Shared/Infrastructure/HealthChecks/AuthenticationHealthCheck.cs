using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SeldonVigil.Api.Shared.Infrastructure.HealthChecks;

public class AuthenticationHealthCheck(IConfiguration configuration, IHostEnvironment hostEnvironment)
    : IHealthCheck
{
    private readonly bool _isDevelopment = hostEnvironment.IsDevelopment();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Development validates against locally-issued tokens (see
            // ServiceRegistration.AddSeldonVigilAuthentication) - only require the shared
            // signing key, independent of the DevAuth toggle.
            if (_isDevelopment)
            {
                var devAuthSecretKey = configuration["DevAuth:SecretKey"];
                return string.IsNullOrWhiteSpace(devAuthSecretKey)
                    ? HealthCheckResult.Unhealthy(
                        "DevAuth:SecretKey is not configured for development authentication",
                        data: new Dictionary<string, object> { ["authMode"] = "LocalDev", ["secretKey"] = "missing" })
                    : HealthCheckResult.Healthy(
                        "Local development authentication configured",
                        data: new Dictionary<string, object>
                        {
                            ["authMode"] = "LocalDev",
                            ["issuer"] = configuration["DevAuth:Issuer"] ?? "seldonvigil-dev"
                        });
            }

            var devAuthEnabled = configuration.GetValue<bool>("DevAuth:Enabled", false);

            // In development with DevAuth enabled, check DevAuth configuration
            // For production/staging, check OIDC configuration
            var authority = configuration["Authentication:Authority"];
            var audience = configuration["Authentication:Audience"];

            if (string.IsNullOrWhiteSpace(authority))
            {
                return HealthCheckResult.Unhealthy(
                    "Authentication:Authority is not configured",
                    data: new Dictionary<string, object>
                    {
                        ["authMode"] = "OIDC",
                        ["authority"] = "missing",
                        ["audience"] = audience ?? "missing"
                    });
            }

            if (string.IsNullOrWhiteSpace(audience))
            {
                return HealthCheckResult.Unhealthy(
                    "Authentication:Audience is not configured",
                    data: new Dictionary<string, object>
                    {
                        ["authMode"] = "OIDC",
                        ["authority"] = authority,
                        ["audience"] = "missing"
                    });
            }

            // Try to validate the OIDC discovery endpoint
            try
            {
                var issuer = authority;
                if (!issuer.EndsWith("/.well-known/openid-configuration"))
                {
                    issuer = issuer.TrimEnd('/') + "/.well-known/openid-configuration";
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await httpClient.GetAsync(issuer, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Degraded(
                        $"OIDC discovery endpoint returned {response.StatusCode}",
                        data: new Dictionary<string, object>
                        {
                            ["authMode"] = "OIDC",
                            ["authority"] = authority,
                            ["audience"] = audience,
                            ["discoveryUrl"] = issuer,
                            ["statusCode"] = (int)response.StatusCode
                        });
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded(
                    $"Failed to connect to OIDC discovery endpoint: {ex.Message}",
                    data: new Dictionary<string, object>
                    {
                        ["authMode"] = "OIDC",
                        ["authority"] = authority,
                        ["audience"] = audience,
                        ["error"] = ex.Message
                    });
            }

            return HealthCheckResult.Healthy(
                "OIDC authentication configuration is valid",
                data: new Dictionary<string, object>
                {
                    ["authMode"] = "OIDC",
                    ["authority"] = authority,
                    ["audience"] = audience
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Authentication health check failed: {ex.Message}",
                exception: ex);
        }
    }
}
