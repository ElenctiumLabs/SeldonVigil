using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Json;
using OpenTelemetry.Metrics;
using SeldonVigil.Api.Modules.Analytics.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.AIReasoning.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.CostIntelligence;
using SeldonVigil.Api.Modules.Domain;
using SeldonVigil.Api.Modules.Governance.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Identity;
using SeldonVigil.Api.Modules.Identity.Api.Endpoints;
using SeldonVigil.Api.Modules.Integration.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Observability;
using SeldonVigil.Api.Modules.Security;
using SeldonVigil.Api.Shared.Infrastructure.Authentication;
using SeldonVigil.Api.Shared.Infrastructure.ExceptionHandling;
using SeldonVigil.Api.Shared.Infrastructure.RateLimiting;

namespace SeldonVigil.Api.Shared.Infrastructure.Configuration;

public static class ConfigureServices
{
    public static IServiceCollection AddSeldonVigilServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // String enums on every minimal-API JSON contract
        services.Configure<JsonOptions>(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Dev auth options
        services.Configure<DevAuthOptions>(
            configuration.GetSection(DevAuthOptions.SectionName));

        // Configuration validation
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();

        // API versioning
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"));
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        // Rate limiting
        services.AddSeldonVigilRateLimiting(configuration);

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("SeldonVigilCors", policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod();

                var allowedOrigins = configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? [];

                if (allowedOrigins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false);
                }
                else
                {
                    policy.WithOrigins(allowedOrigins);
                }
            });
        });

        // Module services
        services
            .AddSeldonVigilIdentityModule()
            .AddSeldonVigilSecurityModule()
            .AddSeldonVigilCostIntelligenceModule()
            .AddSeldonVigilObservabilityModule()
            .AddSeldonVigilDomain()
            .AddSeldonVigilSharedInfrastructure(configuration)
            .AddSeldonVigilAuthentication(configuration)
            .AddSeldonVigilAuthorization()
            .AddSeldonVigilCaching(configuration)
            .AddSeldonVigilMessaging(configuration)
            .AddSeldonVigilTelemetry(configuration)
            .AddProblemDetails()
            .AddExceptionHandler<ValidationExceptionHandler>();

        // Module repositories not yet registered via module extensions
        services.AddScoped<NotificationRepository>();
        services.AddScoped<RootCauseAnalysisRepository>();
        services.AddScoped<GovernancePolicyRepository>();
        services.AddScoped<CertificationPipelineRepository>();
        services.AddScoped<IntegrationConfigurationRepository>();

        // Health checks
        services.AddSeldonVigilHealthChecks(configuration);

        // Resilience (circuit breakers, retries, timeouts)
        services.AddSeldonVigilResilience(configuration);

        // Prometheus metrics
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddPrometheusExporter());

        return services;
    }
}
