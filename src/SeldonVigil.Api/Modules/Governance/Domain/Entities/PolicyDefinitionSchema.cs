namespace SeldonVigil.Api.Modules.Governance.Domain.Entities;

internal sealed class PolicyDefinitionSchema
{
    public string ApiVersion { get; set; } = "seldonvigil.io/v1";
    public string Kind { get; set; } = "Policy";
    public PolicyMetadata Metadata { get; set; } = new();
    public List<PolicyRule> Rules { get; set; } = [];
}
