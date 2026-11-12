namespace SeldonVigil.SharedKernel.Infrastructure;

public interface ICacheService
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    ValueTask RemoveAsync(string key, CancellationToken ct = default);
    ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default);
}
