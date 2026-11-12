using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SeldonVigil.IntegrationTests.Api;

public class AgentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AgentEndpointsTests(WebApplicationFactory<Program> factory)
    {
            var _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["DevAuth:Enabled"] = "true",
                    ["DevAuth:SecretKey"] = "integration-tests-only-seldonvigil-signing-key-0123456789abcdef",
                    ["DevAuth:Issuer"] = "seldonvigil-test",
                    ["DevAuth:Audience"] = "seldonvigil-api-test"
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task DevTokenEndpointReturnsValidToken()
    {
        // Act
        var response = await _client.GetAsync("/dev/token?hours=24&role=Administrator&sub=test-user");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("token");
        content.Should().Contain("expires");
        content.Should().Contain("subject");
        content.Should().Contain("role");
    }

    [Fact]
    public async Task DevTokenEndpointWithDefaultParametersReturnsTokenWithDefaults()
    {
        // Act
        var response = await _client.GetAsync("/dev/token");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("11111111-1111-1111-1111-111111111111");
        content.Should().Contain("Administrator");
    }

    [Fact]
    public async Task HealthCheckEndpointReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}

