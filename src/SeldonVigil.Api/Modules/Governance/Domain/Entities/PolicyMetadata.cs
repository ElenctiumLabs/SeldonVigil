namespace SeldonVigil.Api.Modules.Governance.Domain.Entities;

internal sealed class PolicyMetadata
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new();
}
