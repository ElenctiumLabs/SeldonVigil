using System.Net;
using FluentAssertions;
using Xunit;

namespace SeldonVigil.ApiTests.EndToEnd;

public class HealthEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task HealthCheckReturnsOk()
    {
        var response = await _client.GetAsync("/healthz");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheckReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/healthz");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
