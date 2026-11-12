using Microsoft.Extensions.Options;

namespace SeldonVigil.Sdk.Configuration;

/// <summary>
/// Client for SeldonVigil connectivity checks.
/// </summary>
public class SeldonVigilConfigurationClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeldonVigilConfigurationClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="options">The configuration options.</param>
    public SeldonVigilConfigurationClient(HttpClient httpClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        var configuration = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(configuration.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
    }

    /// <summary>
    /// Validates connectivity against the API health endpoint (/healthz).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the API reports healthy; otherwise, false.</returns>
    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri("/healthz", UriKind.Relative);
            var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            // The health endpoint returns { status: "Healthy", checks: [...] }.
            var status = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return status.Contains("Healthy", StringComparison.OrdinalIgnoreCase) || status.Length == 0;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
