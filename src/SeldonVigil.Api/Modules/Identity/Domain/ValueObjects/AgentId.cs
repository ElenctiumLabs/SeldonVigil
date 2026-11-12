namespace SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

public readonly record struct AgentId(Guid Value)
{
    public static AgentId New() => new(Guid.NewGuid());
    public static AgentId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
