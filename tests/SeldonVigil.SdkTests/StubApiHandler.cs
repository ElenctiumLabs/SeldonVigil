using System.Net;
using SeldonVigil.Sdk.Agents;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;
using ObsClient = SeldonVigil.Sdk.Observability.ObservabilityClient;
using SecClient = SeldonVigil.Sdk.Security.SecurityClient;

namespace SeldonVigil.SdkTests;

/// <summary>
/// Routes requests by URI substring and returns canned responses, standing in for
/// the real API so clients can be tested without a broker or host.
/// </summary>
internal sealed class StubApiHandler : HttpMessageHandler
{
    private readonly List<(string PathContains, Func<HttpResponseMessage> Respond)> _routes = [];
    public List<(string Method, string Uri)> Requests { get; } = [];

    /// <summary>Body of the most recent JSON request, for asserting payloads.</summary>
    public string? LastJsonBody { get; private set; }

    public static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) => new(code)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
    };

    public void Route(string pathContains, Func<HttpResponseMessage> respond) =>
        _routes.Add((pathContains, respond));

    public void RouteJson(string pathContains, string body) =>
        Route(pathContains, () => Json(body));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri?.PathAndQuery ?? string.Empty;
        Requests.Add((request.Method.Method, uri));
        if (request.Content is not null)
        {
            LastJsonBody = request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        }

        foreach (var (pathContains, respond) in _routes)
        {
            if (uri.Contains(pathContains, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(respond());
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    public static (AgentClient Client, StubApiHandler Handler) CreateAgentClient()
    {
        var handler = new StubApiHandler();
        RouteApiKey(handler);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var auth = new SeldonVigilAuthenticationClient(
            http,
            Microsoft.Extensions.Options.Options.Create(new SeldonVigilAuthenticationOptions
            {
                BaseUrl = "http://localhost/",
                ApiKey = "test-key",
            }));
        var client = new AgentClient(
            http,
            auth,
            Microsoft.Extensions.Options.Options.Create(new SeldonVigilConfigurationOptions { BaseUrl = "http://localhost/" }));
        return (client, handler);
    }

    public static (ObsClient Client, StubApiHandler Handler) CreateObservabilityClient()
    {
        var handler = new StubApiHandler();
        RouteApiKey(handler);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var auth = new SeldonVigilAuthenticationClient(
            http,
            Microsoft.Extensions.Options.Options.Create(new SeldonVigilAuthenticationOptions
            {
                BaseUrl = "http://localhost/",
                ApiKey = "test-key",
            }));
        var client = new ObsClient(
            http,
            auth,
            Microsoft.Extensions.Options.Options.Create(new SeldonVigilConfigurationOptions { BaseUrl = "http://localhost/" }));
        return (client, handler);
    }

    public static (SecClient Client, StubApiHandler Handler) CreateSecurityClient()
    {
        var handler = new StubApiHandler();
        RouteApiKey(handler);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var auth = new SeldonVigilAuthenticationClient(
            http,
            Microsoft.Extensions.Options.Options.Create(new SeldonVigilAuthenticationOptions
            {
                BaseUrl = "http://localhost/",
                ApiKey = "test-key",
            }));
        var client = new SecClient(
            http,
            auth,
            Microsoft.Extensions.Options.Options.Create(new SeldonVigilConfigurationOptions { BaseUrl = "http://localhost/" }));
        return (client, handler);
    }

    private static void RouteApiKey(StubApiHandler handler) =>
        handler.RouteJson("/api/v1/auth/api-key", """{"accessToken":"stub-token","refreshToken":"stub-refresh","tokenType":"Bearer","expiresIn":900}""");
}
