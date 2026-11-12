using System.Diagnostics.CodeAnalysis;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Shared.Infrastructure.Tenancy;

/// <summary>
/// Request-scoped tenant resolution. The resolved value feeds EF Core global
/// query filters — treat it as a security boundary, not a convenience.
/// </summary>
public interface ITenantContext
{
    /// <summary>Resolved tenant, or null when none could be established.</summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Non-null form for EF global query filters: resolves to TenantId.Empty
    /// (matches no rows) when no tenant could be established. Reference THIS
    /// property inside HasQueryFilter lambdas - the getter runs per query.
    /// </summary>
    TenantId CurrentTenant { get; }
}

/// <summary>
/// Resolution priority:
/// 1. JWT <c>tenant_id</c> claim — trusted, wins over anything client-supplied.
/// 2. X-Tenant-ID header / subdomain — accepted for pre-auth flows (login) and
///    service-to-service calls; an authenticated user cannot override their
///    claim via headers because the claim is checked first.
/// 3. Development default tenant when nothing is supplied in Development.
///
/// Fail-closed: null tenant + global query filters = empty result sets.
/// </summary>
internal sealed class HttpTenantContext(IHttpContextAccessor accessor, IHostEnvironment environment) : ITenantContext
{
    internal static readonly Guid DevDefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <inheritdoc />
    public TenantId CurrentTenant => new(TenantId ?? Guid.Empty);

    public Guid? TenantId
    {
        get
        {            var http = accessor.HttpContext;
            if (http is null)
            {
                // Non-HTTP context (worker hosts): only an ambient scope grants
                // visibility. Fail closed otherwise.
                return TenantScope.Current;
            }
            // 1. Authenticated claim always wins.
            if (http.User.Identity?.IsAuthenticated == true)
            {
                var claim = http.User.FindFirst("tenant_id")?.Value;
                if (Guid.TryParse(claim, out var fromClaim))
                {
                    return fromClaim;
                }

                // Authenticated but no tenant claim: do NOT fall back to headers,
                // that would let any authenticated caller browse other tenants.
                return null;
            }

            // 2. Ambient worker scope (background message processing).
            if (TenantScope.Current is { } ambient)
            {
                return ambient;
            }

            // 2. Unauthenticated (login, health, SDK key auth in flight):
            //    header or subdomain is the only signal available.
            var header = http.Request.Headers["X-Tenant-ID"].FirstOrDefault();
            if (Guid.TryParse(header, out var fromHeader))
            {
                return fromHeader;
            }

            var subdomain = ExtractSubdomain(http.Request.Host.Host);
            if (Guid.TryParse(subdomain, out var fromHost))
            {
                return fromHost;
            }

            // 3. Development convenience default.
            if (environment.IsDevelopment())
            {
                return DevDefaultTenantId;
            }

            return null;
        }
    }

    private static string? ExtractSubdomain(string host)
    {
        var parts = host.Split('.');
        return parts.Length > 2 ? parts[0] : null;
    }
}

/// <summary>
/// Ambient tenant override for non-HTTP execution contexts (broker workers,
/// migrations). Set per message: <c>using var _ = TenantScope.Begin(tenantId);</c>.
/// Precedence overall: JWT claim > ambient scope > header > dev default.
/// </summary>
public static class TenantScope
{
    private static readonly AsyncLocal<Guid?> Ambient = new();

    public static Guid? Current => Ambient.Value;

    public static IDisposable Begin(Guid tenantId)
    {
        var previous = Ambient.Value;
        Ambient.Value = tenantId;
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope(Guid? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }
}

public static class TenancyServiceCollectionExtensions
{
    /// <summary>
    /// Registers tenant resolution. REQUIRED in every host that activates any
    /// tenant-scoped DbContext (API and Workers alike) - pooled contexts inject
    /// ITenantContext in their constructor.
    /// </summary>
    public static IServiceCollection AddSeldonVigilTenancy(this IServiceCollection services)
    {
        _ = services.AddHttpContextAccessor();
        return services.AddSingleton<ITenantContext, HttpTenantContext>();
    }
}

[SuppressMessage("Performance", "CA1812", Justification = "Registered via DI")]
internal sealed class NullTenantContext : ITenantContext
{
    public Guid? TenantId => null;
    public TenantId CurrentTenant => new(Guid.Empty);
}
