using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using SeldonVigil.Sdk.CostIntelligence;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;
using Microsoft.Extensions.Options;

namespace SeldonVigil.UnitTests.Sdk;

public class CostIntelligenceClientTests
{
    private readonly CostIntelligenceClient _client;

    public CostIntelligenceClientTests()
    {
        var authHttpClient = new HttpClient(new StubHttpMessageHandler(async (request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = JsonContent.Create(new TokenResponse
            {
                AccessToken = "test-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            });
            return response;
        }))
        {
            BaseAddress = new Uri("https://localhost:5001"),
        };

        var authOptions = Options.Create(new SeldonVigilAuthenticationOptions
        {
            BaseUrl = "https://localhost:5001",
            ApiKey = "test-api-key",
        });

        var authClient = new SeldonVigilAuthenticationClient(authHttpClient, authOptions);

        var options = Options.Create(new SeldonVigilConfigurationOptions
        {
            BaseUrl = "https://localhost:5001",
            ApiVersion = "v1",
        });

        var httpClient = new HttpClient(new StubHttpMessageHandler(async (request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (request.RequestUri?.AbsolutePath.Contains("budgets") == true && request.Method == HttpMethod.Get)
            {
                response.Content = JsonContent.Create(new[]
                {
                    new { id = "123", name = "Test Budget", amount = 1000m, currency = "USD", spent = 500m, createdAt = DateTime.UtcNow }
                });
            }
            else if (request.RequestUri?.AbsolutePath.Contains("budgets") == true && request.Method == HttpMethod.Post)
            {
                response.Content = JsonContent.Create(new { id = "456" });
            }
            else if (request.RequestUri?.AbsolutePath.Contains("dashboard") == true)
            {
                response.Content = JsonContent.Create(new { totalCost = 5000m, trend = new object[0], breakdown = new object[0] });
            }
            else if (request.RequestUri?.AbsolutePath.Contains("recommendations") == true)
            {
                response.Content = JsonContent.Create(new object[0]);
            }
            return response;
        }));

        _client = new CostIntelligenceClient(httpClient, authClient, options);
    }

    [Fact]
    public async Task ListBudgetsAsyncReturnsBudgets()
    {
        // Act
        var budgets = await _client.ListBudgetsAsync();

        // Assert
        _ = budgets.Should().HaveCount(1);
        _ = budgets[0].Name.Should().Be("Test Budget");
        _ = budgets[0].Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateBudgetAsyncReturnsId()
    {
        // Arrange
        var request = new CreateBudgetRequest
        {
            Name = "New Budget",
            Amount = 2000m,
            Currency = "USD",
        };

        // Act
        var id = await _client.CreateBudgetAsync(request);

        // Assert
        _ = id.Should().Be("456");
    }

    [Fact]
    public async Task GetDashboardAsyncReturnsDashboard()
    {
        // Act
        var dashboard = await _client.GetDashboardAsync();

        // Assert
        _ = dashboard.Should().NotBeNull();
        _ = dashboard.TotalCost.Should().Be(5000m);
    }
}

internal class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}
