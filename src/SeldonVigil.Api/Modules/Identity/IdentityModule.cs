using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Infrastructure;
using SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Identity;

internal static class IdentityModule
{
    public static IServiceCollection AddSeldonVigilIdentityModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddSingleton<ITokenService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var options = new TokenOptions
            {
                Issuer = config["Identity:Token:Issuer"] ?? "seldonvigil",
                Audience = config["Identity:Token:Audience"] ?? "seldonvigil-api",
                SigningKey = config["Identity:Token:SigningKey"]
                    ?? config["DevAuth:SecretKey"]
                    ?? throw new InvalidOperationException(
                        "Identity:Token:SigningKey must be configured in production."),
                AccessTokenLifetime = TimeSpan.FromMinutes(
                    config.GetValue("Identity:Token:AccessTokenLifetimeMinutes", 15)),
                RefreshTokenLifetime = TimeSpan.FromDays(
                    config.GetValue("Identity:Token:RefreshTokenLifetimeDays", 7))
            };
            return new JwtTokenService(options);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();

        return services;
    }
}
