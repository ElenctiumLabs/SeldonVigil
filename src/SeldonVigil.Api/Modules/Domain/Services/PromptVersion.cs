namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record PromptVersion
{
    public string Name { get; init; } = string.Empty;
    public int Version { get; init; }
    public string Template { get; init; } = string.Empty;
    public string SystemPrompt { get; init; } = string.Empty;
    public ModelConfiguration ModelConfig { get; init; } = new();
    public PromptStatus Status { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
}
