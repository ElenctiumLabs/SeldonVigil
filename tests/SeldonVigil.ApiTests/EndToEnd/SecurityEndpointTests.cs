using System.Net;
using FluentAssertions;
using Xunit;

namespace SeldonVigil.ApiTests.EndToEnd;

public class SecurityEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task ListPoliciesUnauthorizedReturns401()
    {
        var response = await _client.GetAsync("/api/v1/security/policies");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPoliciesAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/security/policies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboardUnauthorizedReturns401()
    {
        var response = await _client.GetAsync("/api/v1/security/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/security/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListViolationsAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/security/violations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
