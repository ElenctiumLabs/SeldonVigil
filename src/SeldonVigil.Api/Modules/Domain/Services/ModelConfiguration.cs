namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ModelConfiguration
{
    public string DefaultModel { get; init; } = "gpt-4o";
    public float Temperature { get; init; } = 0.3f;
    public int MaxTokens { get; init; } = 4000;
    public Dictionary<string, object> Parameters { get; init; } = new();
}
