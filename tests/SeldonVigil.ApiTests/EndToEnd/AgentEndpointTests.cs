using System.Net;
using FluentAssertions;
using Xunit;

namespace SeldonVigil.ApiTests.EndToEnd;

public class AgentEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task ListAgentsUnauthorizedReturns401()
    {
        var response = await _client.GetAsync("/api/v1/agents");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListAgentsAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/agents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAgentUnauthorizedReturns401()
    {
        var response = await _client.GetAsync($"/api/v1/agents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAgentAuthorizedNotFoundReturns404()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/v1/agents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
