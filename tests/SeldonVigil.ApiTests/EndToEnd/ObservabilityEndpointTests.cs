using System.Net;
using FluentAssertions;
using Xunit;

namespace SeldonVigil.ApiTests.EndToEnd;

public class ObservabilityEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task ListTracesUnauthorizedReturns401()
    {
        var response = await _client.GetAsync("/api/v1/observability/traces");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTracesAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/observability/traces");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTraceAuthorizedNotFoundReturns404()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/v1/observability/traces/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
