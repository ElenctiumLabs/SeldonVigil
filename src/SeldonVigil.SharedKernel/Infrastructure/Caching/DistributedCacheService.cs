using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace SeldonVigil.SharedKernel.Infrastructure.Caching;

/// <summary>
/// ICacheService over <see cref="IDistributedCache"/>. Backed by in-memory
/// cache by default, or any RESP-compatible server (Redis, Garnet) when
/// Cache:Provider=Redis.
/// </summary>
public sealed class DistributedCacheService(
    IDistributedCache cache)
    : ICacheService
{
    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await cache.GetStringAsync(key, ct).ConfigureAwait(false);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    public async ValueTask SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var serialized = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }

        await cache.SetStringAsync(key, serialized, options, ct).ConfigureAwait(false);
    }

    public async ValueTask RemoveAsync(string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct).ConfigureAwait(false);
    }

    public async ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var value = await cache.GetStringAsync(key, ct).ConfigureAwait(false);
        return value != null;
    }
}
