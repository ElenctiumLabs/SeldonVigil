using System.Security.Claims;
using System.Threading.RateLimiting;

namespace SeldonVigil.Api.Shared.Infrastructure.RateLimiting;

public static class RateLimitingConfiguration
{
    public static void AddSeldonVigilRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global limiter: per-tenant token bucket
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var identity = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User?.FindFirst("sub")?.Value
                    ?? context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                    ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(identity, _ =>
                {
                    var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
                    var queueLimit = configuration.GetValue("RateLimiting:QueueLimit", 10);
                    var replenishPeriodSec = configuration.GetValue("RateLimiting:ReplenishmentPeriodSeconds", 1);
                    var tokensPerPeriod = configuration.GetValue("RateLimiting:TokensPerPeriod", 10);

                    return new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = permitLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimit,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(replenishPeriodSec),
                        TokensPerPeriod = tokensPerPeriod,
                        AutoReplenishment = true
                    };
                });
            });

            // Endpoint-specific: authentication endpoints (stricter)
            options.AddPolicy("auth", context =>
            {
                var identity = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(identity, _ =>
                    new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = configuration.GetValue("RateLimiting:Auth:TokenLimit", 10),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = configuration.GetValue("RateLimiting:Auth:QueueLimit", 2),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(
                            configuration.GetValue("RateLimiting:Auth:ReplenishmentPeriodSeconds", 60)),
                        TokensPerPeriod = configuration.GetValue("RateLimiting:Auth:TokensPerPeriod", 5),
                        AutoReplenishment = true
                    });
            });

            // Endpoint-specific: write operations (moderate)
            options.AddPolicy("write", context =>
            {
                var identity = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User?.FindFirst("sub")?.Value
                    ?? context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                    ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(identity, _ =>
                    new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = configuration.GetValue("RateLimiting:Write:TokenLimit", 30),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = configuration.GetValue("RateLimiting:Write:QueueLimit", 5),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(
                            configuration.GetValue("RateLimiting:Write:ReplenishmentPeriodSeconds", 1)),
                        TokensPerPeriod = configuration.GetValue("RateLimiting:Write:TokensPerPeriod", 5),
                        AutoReplenishment = true
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue.TotalSeconds
                    : 1;

                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter).ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please retry after the specified delay.",
                    retryAfterSeconds = (int)retryAfter
                }, cancellationToken).ConfigureAwait(false);
            };
        });
    }

    public static void UseSeldonVigilRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
    }
}
