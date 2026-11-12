namespace SeldonVigil.Sdk.Authentication;

/// <summary>
/// Represents a token response from the authentication endpoint.
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the token type (e.g., "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Gets or sets the token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the expiration date/time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the scope of the token.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Determines whether the token is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Determines whether the token is about to expire within the specified buffer.
    /// </summary>
    /// <param name="bufferMinutes">The buffer in minutes.</param>
    public bool IsExpiringSoon(int bufferMinutes = 5) => 
        DateTime.UtcNow >= ExpiresAt.AddMinutes(-bufferMinutes);
}
