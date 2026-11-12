namespace SeldonVigil.Api.Modules.Governance.Domain.Entities;

internal sealed class PipelineStage(string name, string description, bool isRequired)
{
    public string Name { get; init; } = name;
    public string Description { get; init; } = description;
    public bool IsRequired { get; init; } = isRequired;
    public StageStatus Status { get; set; } = StageStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Output { get; set; }
}
