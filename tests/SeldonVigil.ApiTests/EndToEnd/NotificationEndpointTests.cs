using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace SeldonVigil.ApiTests.EndToEnd;

public class NotificationEndpointTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task ListNotificationsUnauthorizedReturns401()
    {
        var response = await _client.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListNotificationsAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnreadNotificationsAuthorizedReturnsOk()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/notifications/unread");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAsReadInvalidIdReturnsBadRequest()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.PatchAsJsonAsync("/api/v1/notifications/not-a-guid/read", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
