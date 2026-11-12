using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SeldonVigil.Api.Modules.Domain.Services;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Application.Behaviors;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.Shared.Domain.Saga;
using SeldonVigil.Shared.Infrastructure.Saga;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using SeldonVigil.Api.Shared.Infrastructure.VectorSearch;
using SeldonVigil.SharedKernel.Infrastructure;
using SeldonVigil.SharedKernel.Infrastructure.Caching;
using SeldonVigil.SharedKernel.Infrastructure.Messaging;
using SeldonVigil.SharedKernel.Infrastructure.Outbox;

namespace SeldonVigil.Api.Shared.Infrastructure;

internal static class ServiceRegistration
{
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions.AddDbContextPool<TContext>(Action<IServiceProvider, DbContextOptionsBuilder>, Int32)")]
    [RequiresDynamicCode("Calls Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOptions>(IConfiguration)")]
    public static IServiceCollection AddSeldonVigilSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MediatR
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssemblyContaining<Program>();
            _ = cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            _ = cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            _ = cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        // FluentValidation
        _ = services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

         // DateTime provider
         _ = services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

         // HTTP Context Accessor (required for HttpRequestContext)
         _ = services.AddHttpContextAccessor();

        // Request context
        _ = services.AddScoped<IRequestContext, HttpRequestContext>();

        // Tenancy — singleton is safe: HttpTenantContext reads the current
        // HttpContext dynamically (AsyncLocal) on every access, so pooled
        // DbContexts resolve fresh per-request tenant values through it.
        _ = services.AddSeldonVigilTenancy();

        // PostgreSQL — single consolidated DbContext for all modules

        _ = services.AddDbContextPool<SeldonVigilDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL");
            _ = options.UseNpgsql(connectionString, npgsql =>
            {
                _ = npgsql.MigrationsAssembly("SeldonVigil.Api");
                _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            });
            _ = options.EnableSensitiveDataLogging(false);
        });

        // Saga persistence
        _ = services.AddScoped<ISagaRepository, PostgresSagaRepository>();
        _ = services.AddScoped<ISagaOrchestrator, SagaOrchestrator>();
        _ = services.AddHostedService<SagaRecoveryService>();

        // Saga steps
        _ = services.AddScoped<ISagaStep, SeldonVigil.Shared.Domain.Saga.AgentActivation.RegisterAgentStep>();
        _ = services.AddScoped<ISagaStep, SeldonVigil.Shared.Domain.Saga.AgentActivation.ApplySecurityPoliciesStep>();
        _ = services.AddScoped<ISagaDefinition, SeldonVigil.Shared.Domain.Saga.AgentActivation.AgentActivationSagaDefinition>();
        _ = services.AddScoped<ISagaRegistry, SagaRegistry>();

        // Distributed cache — optional. Default is in-process memory.
        // "Redis" selects a Redis-protocol server; Garnet (Microsoft's RESP server)
        // is the recommended image and requires no code change.
        var cacheProvider = configuration["Cache:Provider"] ?? "Memory";
        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            _ = services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "SeldonVigil:";
            });
        }
        else
        {
            _ = services.AddDistributedMemoryCache();
        }
        _ = services.AddSingleton<ICacheService, DistributedCacheService>();

        // Broker
        _ = services.Configure<BrokerConsumerOptions>(
            configuration.GetSection(BrokerConsumerOptions.SectionName));
        _ = services.Configure<BrokerSecurityOptions>(
            configuration.GetSection(BrokerSecurityOptions.SectionName));
        var brokerBootstrap = BrokerBootstrapServers.Normalize(
            configuration.GetConnectionString("Broker") ?? throw new InvalidOperationException());
        _ = services.AddSingleton<IBrokerConsumerFactory>(sp =>
            new BrokerConsumerFactory(brokerBootstrap,
                sp.GetRequiredService<IOptions<BrokerSecurityOptions>>().Value));
        _ = services.AddSingleton<IEventBus, EventBus>();
        _ = services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = BrokerClientSecurity.Apply(new ProducerConfig
            {
                BootstrapServers = brokerBootstrap,
                ClientId = "seldonvigil-api",
                CompressionType = CompressionType.Lz4,
                LingerMs = 5,
                BatchSize = 32768
            }, sp.GetRequiredService<IOptions<BrokerSecurityOptions>>().Value);
            return new ProducerBuilder<string, string>(config).Build();
        });

        // Vector store — pgvector on the primary Postgres. No extra service to
        // operate; the NpgsqlDataSource is registered by DomainModule.
        _ = services.AddSingleton<IVectorStore, PgVectorStore>();

        // AI (optional - requires valid API key)
        var openAiApiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAiApiKey))
        {
            _ = services.AddSingleton<IChatClient>(_ => new OpenAIClient(openAiApiKey)
                .GetChatClient("o3-mini")
                .AsIChatClient());

            _ = services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new OpenAIClient(openAiApiKey)
                .GetEmbeddingClient("text-embedding-3-large")
                .AsIEmbeddingGenerator());
        }
        else
        {
            // Fails fast at call time with a clear message instead of crashing app startup.
            _ = services.AddSingleton<IChatClient>(_ => new UnconfiguredChatClient("OpenAI:ApiKey"));
            _ = services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
                _ => new UnconfiguredEmbeddingGenerator("OpenAI:ApiKey"));
        }

        // Long-term memory (requires embeddings; vector store registered above)
        _ = services.AddScoped<LongTermMemoryService>();

        // OpenTelemetry is registered via AddSeldonVigilTelemetry (SeldonVigil.Telemetry).
        // Prometheus scrape endpoint is mapped in Program.cs.
        // Resilience is registered via AddSeldonVigilResilience (ConfigureResilience.cs).

        return services;
    }

    public static IServiceCollection AddSeldonVigilAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JwtBearerOptions are configured lazily via DI so the environment and DevAuth
        // flags are read from the FINAL merged configuration at first resolution.
        // Eager evaluation here would miss overrides added later (e.g. WebApplicationFactory
        // test hosts injecting in-memory configuration after Program.Main registration).
        _ = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        _ = services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration, IHostEnvironment>((options, cfg, hostEnvironment) =>
        {
            var isDevelopment = hostEnvironment.IsDevelopment();
            var devAuthEnabled = cfg.GetValue<bool>("DevAuth:Enabled", false);

            // In development with DevAuth enabled, use DevAuth settings for token validation
            if (isDevelopment && devAuthEnabled)
            {
                var devAuthSecretKey = cfg["DevAuth:SecretKey"];
                var devAuthIssuer = cfg["DevAuth:Issuer"];
                var devAuthAudience = cfg["DevAuth:Audience"];

                if (string.IsNullOrWhiteSpace(devAuthSecretKey))
                {
                    throw new InvalidOperationException("DevAuth:SecretKey must be configured when DevAuth is enabled in development.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = devAuthIssuer,
                    ValidAudience = devAuthAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(devAuthSecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            }
            else if (isDevelopment)
            {
                // Development without an external IdP: validate locally-issued tokens
                // (login / api-key / /dev-token) against the same signing key Identity
                // uses, accepting both issuer spellings. This keeps debugging frictionless
                // regardless of the DevAuth toggle - no OIDC provider required.
                var signingKey = cfg["DevAuth:SecretKey"];
                if (string.IsNullOrWhiteSpace(signingKey))
                {
                    throw new InvalidOperationException(
                        "DevAuth:SecretKey must be configured in appsettings.Development.json for local development authentication.");
                }

                var issuers = new[]
                {
                    cfg["DevAuth:Issuer"],
                    cfg["Identity:Token:Issuer"],
                    "seldonvigil",
                    "seldonvigil-dev"
                }.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToArray();

                var audiences = new[]
                {
                    cfg["DevAuth:Audience"],
                    cfg["Identity:Token:Audience"],
                    "seldonvigil-api"
                }.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToArray();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuers = issuers,
                    ValidAudiences = audiences,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            }
            else
            {
                // Use OIDC settings for production/staging - no symmetric key fallback
                var authority = cfg["Authentication:Authority"];
                if (string.IsNullOrWhiteSpace(authority))
                {
                    throw new InvalidOperationException("Authentication:Authority must be configured in production/staging environments.");
                }

                options.Authority = authority;
                options.Audience = cfg["Authentication:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            }
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) &&
                        context.HttpContext.WebSockets.IsWebSocketRequest)
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        _ = services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin", "super_admin"));
            options.AddPolicy("Engineer", policy => policy.RequireRole("engineer", "admin", "super_admin"));
            options.AddPolicy("SecurityAnalyst", policy => policy.RequireRole("security_analyst", "admin", "super_admin"));
            options.AddPolicy("Viewer", policy => policy.RequireRole("viewer", "engineer", "security_analyst", "admin", "super_admin"));
        });

        return services;
    }

    public static IServiceCollection AddSeldonVigilCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services.AddOutputCache(options =>
        {
            options.AddPolicy("Short", builder => builder.Expire(TimeSpan.FromMinutes(1)));
            options.AddPolicy("Medium", builder => builder.Expire(TimeSpan.FromMinutes(5)));
            options.AddPolicy("Long", builder => builder.Expire(TimeSpan.FromMinutes(30)));
        });

        return services;
    }

    public static IServiceCollection AddSeldonVigilAuthorization(
        this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddSeldonVigilMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Outbox pattern for reliable event publishing
        _ = services.AddScoped<IOutboxRepository, PostgresOutboxRepository>();
        _ = services.AddScoped<OutboxPublisher>();
        _ = services.AddHostedService<OutboxProcessorService>();

        return services;
    }

    public static IServiceCollection AddSeldonVigilTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}

