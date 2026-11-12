using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using SeldonVigil.Api.Modules.Identity.Api.Endpoints;
using SeldonVigil.Api.Modules.Security.Api.Endpoints;
using SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;
using SeldonVigil.Api.Modules.Observability.Api.Endpoints;
using SeldonVigil.Api.Modules.Analytics.Api.Endpoints;
using SeldonVigil.Api.Modules.Domain.Api.Endpoints;
using SeldonVigil.Api.Modules.Messaging.Api.Endpoints;
using SeldonVigil.Api.Modules.AIReasoning.Api.Endpoints;
using SeldonVigil.Api.Modules.Governance.Api.Endpoints;
using SeldonVigil.Api.Modules.Integration.Api.Endpoints;

namespace SeldonVigil.Api.Shared.Infrastructure.Configuration;

public static class ConfigureEndpoints
{
    public static void MapSeldonVigilEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Module endpoints
        app.MapAuthEndpoints();
        app.MapAgentEndpoints();
        app.MapSecurityEndpoints();
        app.MapCostEndpoints();
        app.MapObservabilityEndpoints();
        app.MapNotificationEndpoints();
        app.MapSettingsEndpoints();
        app.MapBrokerEndpoints();
        app.MapAIReasoningEndpoints();
        app.MapGovernanceEndpoints();
        app.MapIntegrationEndpoints();


        // Development helper: issue a test JWT when no external authority is configured
        // WARNING: This endpoint should NEVER be available in production builds
        if (app.Environment.IsDevelopment())
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use LoggerMessage delegates", Justification = "Single log call in static extension method, not in hot path")]
            static void LogDevTokenEnabled(ILogger logger) => logger.LogWarning("Development token endpoint enabled. DO NOT use in production.");
            
            LogDevTokenEnabled(app.Logger);
            app.MapGet("/dev/token", (IOptions<DevAuthOptions> devOpts, int? hours, string? role, string? sub) =>
            {
                var opts = devOpts.Value;
                var now = DateTime.UtcNow;
                // Sub must be a valid GUID: IRequestContext.UserId parses it as one
                // and handlers require a non-null UserId.
                var subject = string.IsNullOrWhiteSpace(sub) ? "11111111-1111-1111-1111-111111111111" : sub;
                var userRole = string.IsNullOrWhiteSpace(role) ? "Administrator" : role;
                var ttlHours = hours.GetValueOrDefault(24);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, subject),
                    new Claim("name", "Developer User"),
                    new Claim("role", userRole),
                    // Tenant claim required by tenant-scoped query filters; without
                    // it authenticated requests resolve to no tenant (fail closed).
                    new Claim("tenant_id", Tenancy.HttpTenantContext.DevDefaultTenantId.ToString())
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SecretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: opts.Issuer,
                    audience: opts.Audience,
                    claims: claims,
                    notBefore: now,
                    expires: now.AddHours(ttlHours),
                    signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                return Results.Ok(new { token = jwt, expires = token.ValidTo, subject, role = userRole });
            }).AllowAnonymous();
        }
    }
}
