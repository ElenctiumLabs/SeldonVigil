namespace SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

internal sealed record AgentTool
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
    public bool Required { get; init; }
}
