using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;

namespace SeldonVigil.Sdk.Analytics;

/// <summary>
/// Client for notification and analytics operations.
/// </summary>
public class AnalyticsClient
{
    private readonly HttpClient _httpClient;
    private readonly SeldonVigilAuthenticationClient _authClient;
    private readonly SeldonVigilConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send requests to the SeldonVigil API.</param>
    /// <param name="authClient">The authentication client used to acquire access tokens.</param>
    /// <param name="options">The SeldonVigil configuration options that supply the API base URL and version.</param>
    public AnalyticsClient(HttpClient httpClient, SeldonVigilAuthenticationClient authClient, IOptions<SeldonVigilConfigurationOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    /// <summary>
    /// Lists all notifications.
    /// </summary>
    /// <param name="unreadOnly">When <c>true</c>, only unread notifications are returned; otherwise, all notifications are returned.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of notifications.</returns>
    public async Task<List<Notification>> ListNotificationsAsync(bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var endpoint = unreadOnly
            ? $"api/{_options.ApiVersion}/notifications/unread"
            : $"api/{_options.ApiVersion}/notifications";

        using var response = await SendAsync(HttpMethod.Get, endpoint, null, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<NotificationWire>>(SdkJson.Options, cancellationToken).ConfigureAwait(false);
        return result?.Select(MapNotification).ToList() ?? [];
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="notificationId">The notification ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Patch, $"api/{_options.ApiVersion}/notifications/{notificationId}/read", null, cancellationToken).ConfigureAwait(false);
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

    private static Notification MapNotification(NotificationWire wire) => new()
    {
        Id = wire.Id ?? string.Empty,
        Type = wire.Type ?? string.Empty,
        Title = wire.Title ?? string.Empty,
        Message = wire.Message ?? string.Empty,
        AgentId = wire.AgentId,
        Severity = wire.Severity ?? string.Empty,
        CreatedAt = wire.CreatedAt,
        Read = wire.Read
    };
}

/// <summary>
/// Represents a notification in the SeldonVigil platform.
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ID of the agent that raised the notification, or <see langword="null"/> if it is not associated with an agent.
    /// </summary>
    public string? AgentId { get; set; }
    /// <summary>
    /// Gets or sets the notification severity.
    /// </summary>
    public string Severity { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the timestamp at which the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the notification has been read.
    /// </summary>
    public bool Read { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class NotificationWire
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? AgentId { get; set; }
    public string? Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Read { get; set; }
}
