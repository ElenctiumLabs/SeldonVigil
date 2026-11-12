using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Shared.Infrastructure.Security;

internal sealed partial class KeyVaultSecretResolver : ISecretResolver
{
    private readonly SecretClient _secretClient;
    private readonly ICacheService _cache;
    private readonly ILogger<KeyVaultSecretResolver> _logger;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Secret {SecretName} retrieved from cache")]
    private static partial void LogSecretCacheHit(ILogger logger, string secretName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Secret {SecretName} retrieved from Key Vault")]
    private static partial void LogSecretRetrievedFromVault(ILogger logger, string secretName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Secret {SecretName} not found in Key Vault")]
    private static partial void LogSecretNotFound(ILogger logger, string secretName);

    public KeyVaultSecretResolver(
        IConfiguration configuration,
        ICacheService cache,
        ILogger<KeyVaultSecretResolver> logger)
    {
        var keyVaultUri = configuration["Azure:KeyVaultUri"]
            ?? throw new InvalidOperationException("Key Vault URI not configured");

        _secretClient = new SecretClient(
            new Uri(keyVaultUri),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeAzureCliCredential = false,
                ExcludeManagedIdentityCredential = false
            }));

        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        var cacheKey = $"seldonvigil:secret:{secretName}";

        var cached = await _cache.GetAsync<string>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            LogSecretCacheHit(_logger, secretName);
            return cached;
        }

        try
        {
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: ct)
                .ConfigureAwait(false);

            var secretValue = response.Value.Value;

            // Cache with short TTL (secrets rotate frequently)
            await _cache.SetAsync(cacheKey, secretValue, TimeSpan.FromMinutes(5), ct)
                .ConfigureAwait(false);

            LogSecretRetrievedFromVault(_logger, secretName);

            return secretValue;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            LogSecretNotFound(_logger, secretName);
            throw new SecretNotFoundException($"Secret '{secretName}' not found", ex);
        }
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public async Task<T> GetSecretAsync<T>(string secretName, CancellationToken ct = default) where T : class
    {
        var json = await GetSecretAsync(secretName, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize secret '{secretName}'");
    }
}