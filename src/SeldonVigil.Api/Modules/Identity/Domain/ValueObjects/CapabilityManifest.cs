using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

internal sealed record CapabilityManifest
{
    public IReadOnlyList<AgentTool> Tools { get; init; } = [];
    public IReadOnlyList<DataAccessRule> DataAccess { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public RateLimit RateLimits { get; init; } = new();

    public string ComputeHash()
    {
        var json = JsonSerializer.Serialize(this, CapabilityManifestContext.Default.CapabilityManifest);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
