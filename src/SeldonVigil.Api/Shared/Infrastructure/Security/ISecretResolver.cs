namespace SeldonVigil.Api.Shared.Infrastructure.Security;

internal interface ISecretResolver
{
    Task<string> GetSecretAsync(string secretName, CancellationToken ct = default);
    Task<T> GetSecretAsync<T>(string secretName, CancellationToken ct = default) where T : class;
}
