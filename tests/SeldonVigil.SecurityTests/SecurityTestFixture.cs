using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SeldonVigil.SharedKernel.Infrastructure;
using Xunit;

namespace SeldonVigil.SecurityTests;

public sealed class SecurityTestFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityTestFixture()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["DevAuth:Enabled"] = "true",
                    ["DevAuth:SecretKey"] = "security-tests-only-seldonvigil-signing-key-0123456789abcdef",
                    ["DevAuth:Issuer"] = "seldonvigil-test",
                    ["DevAuth:Audience"] = "seldonvigil-api-test"
                });
            });
            // Tests must not depend on external infrastructure (Redis).
            builder.ConfigureTestServices(services =>
            {
                // Tests must not depend on external infrastructure (Redis, broker).
                services.RemoveAll<ICacheService>();
                services.AddSingleton<ICacheService, InMemoryTestCache>();
                services.RemoveAll<IHealthCheckPublisher>();
                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    var brokerRegistrations = options.Registrations
                        .Where(r => r.Name == "broker")
                        .ToList();
                    foreach (var registration in brokerRegistrations)
                    {
                        options.Registrations.Remove(registration);
                    }
                });
            });
        });
    }

    public HttpClient CreateClient() => _factory.CreateClient();

    public HttpClient CreateAnonymousClient() => _factory.CreateClient();

    public HttpClient CreateClientWithRole(string role)
    {
        var client = _factory.CreateClient();
        var token = GetDevTokenAsync(client, role: role).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateClientForTenant(string tenantId)
    {
        var client = _factory.CreateClient();
        var token = GetDevTokenAsync(client, sub: "00000000-0000-0000-0000-000000000001").GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        return client;
    }

    private static async Task<string> GetDevTokenAsync(
        HttpClient client, string role = "Administrator", string sub = "test-user")
    {
        var response = await client.GetAsync($"/dev/token?hours=24&role={role}&sub={sub}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<DevTokenResponse>();
        return json?.Token ?? throw new InvalidOperationException("Failed to obtain dev token");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    private sealed record DevTokenResponse(string Token, DateTime Expires, string Subject, string Role);

    internal sealed class InMemoryTestCache : ICacheService
    {
        private sealed record Entry(object? Value, DateTime? ExpiresAt);

        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt is null || entry.ExpiresAt.Value > DateTime.UtcNow)
                {
                    return new ValueTask<T?>((T?)entry.Value);
                }
                _ = _entries.TryRemove(key, out _);
            }
            return new ValueTask<T?>(default(T));
        }

        public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;
            _entries[key] = new Entry(value, expiresAt);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key, CancellationToken ct = default)
        {
            _ = _entries.TryRemove(key, out _);
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return new ValueTask<bool>(_entries.ContainsKey(key));
        }
    }
}
