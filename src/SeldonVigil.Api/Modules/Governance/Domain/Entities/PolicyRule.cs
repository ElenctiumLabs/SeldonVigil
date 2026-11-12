namespace SeldonVigil.Api.Modules.Governance.Domain.Entities;

internal sealed class PolicyRule
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty; // CEL expression
    public string Action { get; set; } = string.Empty; // allow, deny, audit, warn
    public int Priority { get; set; } = 100;
}
