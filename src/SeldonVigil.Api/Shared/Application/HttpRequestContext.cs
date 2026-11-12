using SeldonVigil.Api.Shared.Infrastructure.Tenancy;

namespace SeldonVigil.Api.Shared.Application;

internal sealed class HttpRequestContext(
    IHttpContextAccessor httpContextAccessor,
    ITenantContext tenantContext) : IRequestContext
{
    public Guid CorrelationId
    {
        get
        {
            var correlationId = httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
            return Guid.TryParse(correlationId, out var id) ? id : Guid.NewGuid();
        }
    }

    public Guid? UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?.Items["UserId"]?.ToString();
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Tenant resolution is delegated to <see cref="ITenantContext"/> so every
    /// consumer (query handlers AND EF query filters) sees the same value with
    /// the same trust rules: JWT claim first, spoofable headers only pre-auth.
    /// </summary>
    public string? TenantId => tenantContext.TenantId?.ToString();

    public string TraceId => httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
}
