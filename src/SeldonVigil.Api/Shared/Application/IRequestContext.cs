namespace SeldonVigil.Api.Shared.Application;

internal interface IRequestContext
{
    Guid CorrelationId { get; }
    Guid? UserId { get; }
    string? TenantId { get; }
    string TraceId { get; }
}
