using Microsoft.Extensions.Options;
using SeldonVigil.Sdk.Authentication;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.SdkTests;

public class AuthClientTests
{
    private static SeldonVigilAuthenticationClient CreateClient(StubApiHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            Options.Create(new SeldonVigilAuthenticationOptions { BaseUrl = "http://localhost/", ApiKey = "test-key" }));

    [Fact]
    public async Task ApiKeyLoginStoresTokenWithExpiry()
    {
        var handler = new StubApiHandler();
        handler.RouteJson("/api/v1/auth/api-key", """{"accessToken":"tok-1","refreshToken":"ref-1","tokenType":"Bearer","expiresIn":900}""");
        var client = CreateClient(handler);

        var token = await client.AuthenticateWithApiKeyAsync();

        token.AccessToken.Should().Be("tok-1");
        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(14));
        (await client.GetAccessTokenAsync()).Should().Be("tok-1");
    }

    [Fact]
    public async Task RefreshMapsLoginResponseShape()
    {
        // /auth/refresh returns the LoginResponse shape: { token, refreshToken, username, roles }.
        var handler = new StubApiHandler();
        handler.RouteJson("/api/v1/auth/api-key", """{"accessToken":"tok-1","refreshToken":"ref-1","expiresIn":900}""");
        handler.RouteJson("/api/v1/auth/refresh", """{"token":"tok-2","refreshToken":"ref-2","username":"svc","roles":["admin"]}""");
        var client = CreateClient(handler);

        await client.AuthenticateWithApiKeyAsync();
        var refreshed = await client.RefreshTokenAsync();

        refreshed.AccessToken.Should().Be("tok-2");
        refreshed.RefreshToken.Should().Be("ref-2");
        refreshed.ExpiresIn.Should().BeGreaterThan(0);
        refreshed.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task LoginWithPasswordMapsLoginResponseShape()
    {
        var handler = new StubApiHandler();
        handler.RouteJson("/api/v1/auth/login", """{"token":"pw-tok","refreshToken":"pw-ref","username":"dev-user","roles":["admin"]}""");
        var client = CreateClient(handler);

        var token = await client.LoginWithPasswordAsync("dev-user", "whatever");

        token.AccessToken.Should().Be("pw-tok");
        token.TokenType.Should().Be("Bearer");
        (await client.GetAccessTokenAsync()).Should().Be("pw-tok");
    }
}
