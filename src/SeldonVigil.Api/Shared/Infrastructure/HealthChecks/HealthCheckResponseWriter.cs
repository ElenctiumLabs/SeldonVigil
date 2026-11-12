using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.CodeAnalysis;

namespace SeldonVigil.Api.Shared.Infrastructure.HealthChecks;

internal static class HealthCheckResponseWriter
{
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync<TValue>(TValue, CancellationToken)")]
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync<TValue>(TValue, CancellationToken)")]
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        };

        return context.Response.WriteAsJsonAsync(result);
    }
}
