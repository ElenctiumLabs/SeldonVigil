namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record AgentTool
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, string> Parameters { get; init; } = new();
}
