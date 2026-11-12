using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Modules.Security.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Security;

internal static class SecurityModule
{
    public static IServiceCollection AddSeldonVigilSecurityModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Repositories (DbContext registered centrally in ServiceRegistration)
        _ = services.AddScoped<ISecurityPolicyRepository, SecurityPolicyRepository>();
        _ = services.AddScoped<ISecurityViolationRepository, SecurityViolationRepository>();

        return services;
    }
}
