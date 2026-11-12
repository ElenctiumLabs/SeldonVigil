namespace SeldonVigil.Api.Shared.Infrastructure.Extensions;

internal sealed partial class TenantResolutionMiddleware(RequestDelegate next, IHostEnvironment environment, ILogger<TenantResolutionMiddleware> logger)
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [LoggerMessage(Level = LogLevel.Debug, Message = "Using default tenant ID for development: {TenantId}")]
    private static partial void LogDefaultTenantUsed(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No tenant ID provided for request: {Path}")]
    private static partial void LogNoTenantId(ILogger logger, string path);

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract tenant ID from header or subdomain
        var tenantId = context.Request.Headers["X-Tenant-ID"].FirstOrDefault() ??
                       ExtractTenantFromHost(context.Request.Host.Host);

        // For development, use a default tenant ID if none is provided
        if (string.IsNullOrEmpty(tenantId))
        {
            if (environment.IsDevelopment())
            {
                tenantId = DefaultTenantId.ToString();
                LogDefaultTenantUsed(logger, tenantId);
            }
            else
            {
                LogNoTenantId(logger, context.Request.Path);
            }
        }

        context.Items["TenantId"] = tenantId;
        await next(context).ConfigureAwait(false);
    }

    private static string? ExtractTenantFromHost(string host)
    {
        // Extract tenant from subdomain format: tenant.example.com
        var parts = host.Split('.');
        return parts.Length > 2 ? parts[0] : null;
    }
}