namespace SeldonVigil.Api.Shared.Infrastructure.Extensions;

internal static class MiddlewareExtensions
{
    public static WebApplication UseRequestContext(this WebApplication app)
    {
        app.UseMiddleware<RequestContextMiddleware>();
        return app;
    }

    public static WebApplication UseTenantResolution(this WebApplication app)
    {
        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }
}
