namespace SeldonVigil.Sdk.Configuration;

/// <summary>
/// Configuration options for the SeldonVigil SDK.
/// </summary>
public class SeldonVigilConfigurationOptions
{
    /// <summary>
    /// Gets or sets the base URL of the SeldonVigil API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8080";

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the API version to use.
    /// </summary>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>
    /// Gets or sets the timeout for API requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable detailed logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Gets or sets whether to validate SSL certificates.
    /// </summary>
    public bool ValidateSsl { get; set; } = true;
}
