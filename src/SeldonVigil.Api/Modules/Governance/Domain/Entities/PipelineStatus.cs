namespace SeldonVigil.Api.Modules.Governance.Domain.Entities;

internal enum PipelineStatus
{
    Pending,
    InProgress,
    AwaitingApproval,
    Approved,
    Rejected,
    Expired
}
