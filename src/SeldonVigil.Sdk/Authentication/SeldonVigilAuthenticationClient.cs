using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SeldonVigil.Sdk.Authentication;

/// <summary>
/// Client for handling authentication with the SeldonVigil API.
/// </summary>
public class SeldonVigilAuthenticationClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationOptions _options;
    private TokenResponse? _currentToken;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="SeldonVigilAuthenticationClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="options">The authentication options.</param>
    public SeldonVigilAuthenticationClient(HttpClient httpClient, IOptions<SeldonVigilAuthenticationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Gets the current access token, authenticating and refreshing as necessary.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The access token.</returns>
    /// <exception cref="InvalidOperationException">Thrown when authentication fails.</exception>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_currentToken == null || _currentToken.IsExpired)
            {
                if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    _currentToken = await AuthenticateWithApiKeyAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (HasOAuth2Credentials())
                {
                    _currentToken = await AuthenticateWithOAuth2Async(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException(
                        "No SeldonVigil credentials configured. Provide an API key or OAuth2 client credentials.");
                }
            }
            else if (_options.AutoRefreshTokens && _currentToken.IsExpiringSoon(_options.TokenRefreshBufferMinutes))
            {
                _ = RefreshTokenInBackground(cancellationToken);
            }

            return _currentToken?.AccessToken ?? string.Empty;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool HasOAuth2Credentials() =>
        !string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.ClientSecret);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1849:Call async methods when in an async method",
        Justification = "Deliberate background refresh; callers already hold a valid token")]
    private async Task RefreshTokenInBackground(CancellationToken cancellationToken)
    {
        try
        {
            await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                          or InvalidOperationException or JsonException)
        {
            // Proactive refresh is best-effort; the next call retries synchronously
            // once the current token actually expires.
            _ = ex;
        }
    }

    /// <summary>
    /// Authenticates with username and password (POST /api/v1/auth/login).
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The token response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when authentication fails.</exception>
    public async Task<TokenResponse> LoginWithPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username,
            password
        }, SdkJson.Options, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await ReadAndStoreTokenAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Authenticates using API key.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The token response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when API key is not configured or authentication fails.</exception>
    public async Task<TokenResponse> AuthenticateWithApiKeyAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("API key is not configured.");
        }

        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/api-key", new
        {
            apiKey = _options.ApiKey
        }, SdkJson.Options, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await ReadAndStoreTokenAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Authenticates using OAuth2 client credentials flow.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The token response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when OAuth2 credentials are not configured or authentication fails.</exception>
    public async Task<TokenResponse> AuthenticateWithOAuth2Async(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("OAuth2 client credentials are not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Authority))
        {
            throw new InvalidOperationException("OAuth2 authority is not configured.");
        }

        var tokenEndpoint = $"{_options.Authority.TrimEnd('/')}/connect/token";

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["scope"] = _options.Scope ?? string.Empty
        });

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken).ConfigureAwait(false);
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize token response.");
        }

        tokenResponse.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        _currentToken = tokenResponse;
        return tokenResponse;
    }

    /// <summary>
    /// Refreshes the current access token using the refresh token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refreshed token response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no refresh token is available or refresh fails.</exception>
    public async Task<TokenResponse> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_currentToken?.RefreshToken == null)
        {
            throw new InvalidOperationException("No refresh token available.");
        }

        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = _currentToken.RefreshToken
        }, SdkJson.Options, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await ReadAndStoreTokenAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads either auth response shape. /auth/api-key returns
    /// {accessToken, refreshToken, expiresIn}; /auth/login and /auth/refresh return
    /// {token, refreshToken, username, roles} — both are normalized here.
    /// </summary>
    private async Task<TokenResponse> ReadAndStoreTokenAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var wire = await response.Content.ReadFromJsonAsync<AuthTokenResponseWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Failed to deserialize token response.");

        var accessToken = wire.AccessToken ?? wire.Token ?? throw new InvalidOperationException("Token response contained no access token.");

        // Login/refresh responses omit expiry; the API's access-token lifetime is 15 min.
        var expiresIn = wire.ExpiresIn ?? 900;

        var tokenResponse = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = wire.RefreshToken,
            TokenType = wire.TokenType ?? "Bearer",
            ExpiresIn = expiresIn,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
        };

        _currentToken = tokenResponse;
        return tokenResponse;
    }

    /// <summary>
    /// Validates a JWT token and returns its claims.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>A dictionary of token claims.</returns>
    /// <exception cref="ArgumentException">Thrown when the token is null or empty.</exception>
    public static IDictionary<string, string> ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        return jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
    }

    /// <summary>
    /// Signs out and clears the current token.
    /// </summary>
    public void SignOut()
    {
        _currentToken = null;
    }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore.Dispose();
        }
    }
}

/// <summary>
/// Union of both auth response wire shapes.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization by System.Net.Http.Json")]
internal sealed class AuthTokenResponseWire
{
    public string? AccessToken { get; set; }

    /// <summary>Alternative access-token field used by /auth/login and /auth/refresh.</summary>
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }

    public string? TokenType { get; set; }

    public int? ExpiresIn { get; set; }
}
