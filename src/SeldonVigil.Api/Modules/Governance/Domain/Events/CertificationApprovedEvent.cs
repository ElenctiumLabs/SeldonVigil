using SeldonVigil.Api.Modules.Governance.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Governance.Domain.Events;

internal sealed record CertificationApprovedEvent(
    CertificationPipelineId PipelineId,
    TenantId TenantId,
    AgentId AgentId,
    DateTime EventTimestamp) : BaseEvent;
