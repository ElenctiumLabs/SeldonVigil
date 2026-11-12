using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace SeldonVigil.ApiTests.EndToEnd;

public class GovernanceEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task ListPoliciesUnauthorizedReturns401()
    {
        var response = await _client.GetAsync("/api/v1/governance/policies");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListPoliciesAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/governance/policies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePolicyAuthorizedReturnsCreated()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var request = new
        {
            Name = "Test Policy",
            Description = "A test governance policy",
            Category = "Security",
            DefinitionYaml = "rules:\n  - name: test\n    action: allow"
        };

        var response = await client.PostAsJsonAsync("/api/v1/governance/policies", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task TriggerCertificationAuthorizedReturnsCreated()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var request = new { AgentId = Guid.NewGuid().ToString() };

        var response = await client.PostAsJsonAsync("/api/v1/governance/certification/trigger", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
