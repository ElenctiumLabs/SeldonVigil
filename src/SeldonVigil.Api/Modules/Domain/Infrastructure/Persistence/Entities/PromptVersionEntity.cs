using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence model for a versioned prompt.
/// </summary>
internal sealed class PromptVersionEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Template { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public ModelConfiguration ModelConfig { get; set; } = new();
    public PromptStatus Status { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
