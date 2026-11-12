using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Domain.Entities;

internal sealed class AgentTrustRelationship : BaseEntity<Guid>
{
    public TenantId TenantId { get; }
    public AgentId SourceAgentId { get; }
    public AgentId TargetAgentId { get; }
    public string RelationshipType { get; private set; } = string.Empty;
    public double TrustScore { get; }

    private AgentTrustRelationship() { }
}
