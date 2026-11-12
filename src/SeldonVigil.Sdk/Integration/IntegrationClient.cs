using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.Integration;

/// <summary>
/// Client for third-party integration management.
/// </summary>
public class IntegrationClient
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="authClient">The authentication client.</param>
    /// <param name="options">The configuration options.</param>
    public IntegrationClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Lists all integrations for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of integrations.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    public async Task<List<SeldonVigilIntegration>> ListIntegrationsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/integrations", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<IntegrationWire>>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Select(MapIntegration).ToList() ?? [];
    }

    /// <summary>
    /// Gets an integration by ID.
    /// </summary>
    /// <param name="integrationId">The integration ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The integration, or null if not found.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when integration ID is not a valid GUID.</exception>
    public async Task<SeldonVigilIntegration?> GetIntegrationAsync(string integrationId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(integrationId, out var guid))
            throw new ArgumentException("Integration ID must be a valid GUID.", nameof(integrationId));

        using var response = await SendAsync(HttpMethod.Get, $"api/{_options.ApiVersion}/integrations/{guid}", null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        var wire = await response.Content.ReadFromJsonAsync<IntegrationWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? null : MapIntegration(wire);
    }

    /// <summary>
    /// Creates a new integration.
    /// </summary>
    /// <param name="request">The integration to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created integration.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<SeldonVigilIntegration> CreateIntegrationAsync(CreateIntegrationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/integrations", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<IntegrationWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to create integration.") : MapIntegration(wire);
    }

    /// <summary>
    /// Activates an integration.
    /// </summary>
    /// <param name="integrationId">The integration ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activated integration.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when integration ID is not a valid GUID.</exception>
    public async Task<SeldonVigilIntegration> ActivateIntegrationAsync(string integrationId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(integrationId, out var guid))
            throw new ArgumentException("Integration ID must be a valid GUID.", nameof(integrationId));

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/integrations/{guid}/activate", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<IntegrationWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to activate integration.") : MapIntegration(wire);
    }

    /// <summary>
    /// Deactivates an integration.
    /// </summary>
    /// <param name="integrationId">The integration ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deactivated integration.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when integration ID is not a valid GUID.</exception>
    public async Task<SeldonVigilIntegration> DeactivateIntegrationAsync(string integrationId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(integrationId, out var guid))
            throw new ArgumentException("Integration ID must be a valid GUID.", nameof(integrationId));

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/integrations/{guid}/deactivate", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var wire = await response.Content.ReadFromJsonAsync<IntegrationWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return wire is null ? throw new InvalidOperationException("Failed to deactivate integration.") : MapIntegration(wire);
    }

    /// <summary>
    /// Tests an integration by sending a test message.
    /// </summary>
    /// <param name="integrationId">The integration ID.</param>
    /// <param name="message">Optional test message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The test result.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when integration ID is not a valid GUID.</exception>
    public async Task<TestIntegrationResult> TestIntegrationAsync(string integrationId, string? message = null, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(integrationId, out var guid))
            throw new ArgumentException("Integration ID must be a valid GUID.", nameof(integrationId));

        using var response = await SendAsync(HttpMethod.Post, $"api/{_options.ApiVersion}/integrations/{guid}/test",
            new { message }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TestIntegrationResultWire>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return new TestIntegrationResult
        {
            Success = result?.Success ?? false,
            Message = result?.Message ?? string.Empty,
            Duration = result?.Duration ?? TimeSpan.Zero
        };
    }

    /// <summary>
    /// Deletes an integration.
    /// </summary>
    /// <param name="integrationId">The integration ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="ArgumentException">Thrown when integration ID is not a valid GUID.</exception>
    public async Task DeleteIntegrationAsync(string integrationId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(integrationId, out var guid))
            throw new ArgumentException("Integration ID must be a valid GUID.", nameof(integrationId));

        using var response = await SendAsync(HttpMethod.Delete, $"api/{_options.ApiVersion}/integrations/{guid}", null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        var token = await _authClient.GetAccessTokenAsync(ct).ConfigureAwait(false);

        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: SdkJson.Options);
        }

        return await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    private static SeldonVigilIntegration MapIntegration(IntegrationWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        TenantId = wire.TenantId ?? string.Empty,
        Name = wire.Name ?? string.Empty,
        Type = wire.Type ?? string.Empty,
        Status = wire.Status ?? string.Empty,
        CreatedAt = wire.CreatedAt,
        LastUsedAt = wire.LastUsedAt,
        ErrorCount = wire.ErrorCount
    };
}

/// <summary>
/// Represents a third-party integration.
/// </summary>
public class SeldonVigilIntegration
{
    /// <summary>
    /// Gets or sets the integration ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the integration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the integration type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the integration status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the last usage timestamp.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public int ErrorCount { get; set; }
}

/// <summary>
/// Request to create a new integration.
/// </summary>
public class CreateIntegrationRequest
{
    /// <summary>
    /// Gets or sets the integration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the integration type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the integration configuration as a JSON string.
    /// </summary>
    public string ConfigurationJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the optional webhook URL.
    /// </summary>
    public string? WebhookUrl { get; set; }
    /// <summary>
    /// Gets or sets the optional API key.
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Result of an integration test.
/// </summary>
public class TestIntegrationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the test succeeded.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the test message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the test duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class IntegrationWire
{
    public string? Id { get; set; }
    public string? TenantId { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int ErrorCount { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class TestIntegrationResultWire
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TimeSpan Duration { get; set; }
}
