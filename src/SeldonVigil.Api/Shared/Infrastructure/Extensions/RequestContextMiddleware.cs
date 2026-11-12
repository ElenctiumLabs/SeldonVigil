using System.Security.Claims;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Shared.Infrastructure.Extensions;

internal class RequestContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
    {
        // Extract request context information.
        // Check both "sub" and ClaimTypes.NameIdentifier: JWT inbound claim mapping may
        // rename the subject claim depending on JwtBearerOptions.MapInboundClaims.
        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var correlationId = context.Request.HttpContext.TraceIdentifier;

        // Store in context items for later use
        context.Items["UserId"] = userId;
        context.Items["CorrelationId"] = correlationId;

        await next(context).ConfigureAwait(false);
    }
}
