namespace SeldonVigil.Sdk.Authentication;

/// <summary>
/// Configuration options for SeldonVigil SDK authentication.
/// </summary>
public class SeldonVigilAuthenticationOptions
{
    /// <summary>
    /// Gets or sets the base URL of the SeldonVigil API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8080";

    /// <summary>
    /// Gets or sets the API key for authentication (alternative to JWT).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the client ID for OAuth2 flow.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for OAuth2 flow.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 authority URL.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 scope.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically refresh tokens when they expire.
    /// </summary>
    public bool AutoRefreshTokens { get; set; } = true;

    /// <summary>
    /// Gets or sets the token refresh buffer in minutes (how long before expiry to refresh).
    /// </summary>
    public int TokenRefreshBufferMinutes { get; set; } = 5;
}
