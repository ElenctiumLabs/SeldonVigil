using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using SeldonVigil.Sdk.Agents;
using SeldonVigil.Sdk.AIReasoning;
using SeldonVigil.Sdk.Analytics;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Configuration;
using SeldonVigil.Sdk.CostIntelligence;
using SeldonVigil.Sdk.Governance;
using SeldonVigil.Sdk.Integration;
using SeldonVigil.Sdk.Observability;
using SeldonVigil.Sdk.OpenTelemetry;
using SeldonVigil.Sdk.Security;

namespace SeldonVigil.Sdk.Extensions;

/// <summary>
/// Extension methods for configuring SeldonVigil SDK services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the SeldonVigil SDK to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAuthOptions"></param>
    /// <param name="configureConfigOptions"></param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSeldonVigilSdk(
        this IServiceCollection services,
        Action<SeldonVigilAuthenticationOptions>? configureAuthOptions = null,
        Action<SeldonVigilConfigurationOptions>? configureConfigOptions = null)
    {
        // Configure authentication options
        if (configureAuthOptions != null)
        {
            services.Configure(configureAuthOptions);
        }
        else
        {
            services.Configure<SeldonVigilAuthenticationOptions>(options =>
            {
                options.BaseUrl = "http://localhost:8080";
                options.AutoRefreshTokens = true;
                options.TokenRefreshBufferMinutes = 5;
            });
        }

        // Configure SDK options
        if (configureConfigOptions != null)
        {
            services.Configure(configureConfigOptions);
        }
        else
        {
            services.Configure<SeldonVigilConfigurationOptions>(options =>
            {
                options.BaseUrl = "http://localhost:8080";
                options.ApiVersion = "v1";
                options.TimeoutSeconds = 30;
                options.MaxRetryAttempts = 3;
                options.EnableDetailedLogging = false;
                options.ValidateSsl = true;
            });
        }

        // Register HTTP clients (timeout from SeldonVigilConfigurationOptions.TimeoutSeconds)
        services.AddHttpClient<SeldonVigilAuthenticationClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<SeldonVigilConfigurationClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<AgentClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<ObservabilityClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<SecurityClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<CostIntelligenceClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<GovernanceClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<AnalyticsClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<AIReasoningClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));
        services.AddHttpClient<IntegrationClient>((sp, client) =>
            client.Timeout = ResolveTimeout(sp));

        // Register SDK clients
        services.AddSingleton<SeldonVigilAuthenticationClient>();
        services.AddSingleton<SeldonVigilConfigurationClient>();
        services.AddSingleton<AgentClient>();
        services.AddSingleton<ObservabilityClient>();
        services.AddSingleton<SecurityClient>();
        services.AddSingleton<CostIntelligenceClient>();
        services.AddSingleton<GovernanceClient>();
        services.AddSingleton<AnalyticsClient>();
        services.AddSingleton<AIReasoningClient>();
        services.AddSingleton<IntegrationClient>();

        return services;
    }

    private static TimeSpan ResolveTimeout(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SeldonVigilConfigurationOptions>>().Value;
        return TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 300));
    }

    /// <summary>
    /// Adds the SeldonVigil SDK with API key authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="baseUrl">The base URL (optional).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSeldonVigilSdkWithApiKey(
        this IServiceCollection services,
        string apiKey,
        string? baseUrl = null)
    {
        return services.AddSeldonVigilSdk(
            authOptions =>
            {
                authOptions.ApiKey = apiKey;
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    authOptions.BaseUrl = baseUrl;
                }
            },
            configOptions =>
            {
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    configOptions.BaseUrl = baseUrl;
                }
            });
    }

    /// <summary>
    /// Adds the SeldonVigil SDK with API key authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="baseUrl">The base URL (optional).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSeldonVigilSdkWithApiKey(
        this IServiceCollection services,
        string apiKey,
        Uri? baseUrl = null)
    {
        return services.AddSeldonVigilSdkWithApiKey(apiKey, baseUrl?.ToString());
    }

    /// <summary>
    /// Adds the SeldonVigil SDK with OAuth2 authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientId">The client ID.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="authority">The OAuth2 authority URL.</param>
    /// <param name="scope">The OAuth2 scope (optional).</param>
    /// <param name="baseUrl">The base URL (optional).</param>
    /// <returns>The service collection for chaining.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "String overload provided for backward compatibility")]
    public static IServiceCollection AddSeldonVigilSdkWithOAuth2(
        this IServiceCollection services,
        string clientId,
        string clientSecret,
        string authority,
        string? scope = null,
        string? baseUrl = null)
    {
        return services.AddSeldonVigilSdk(
            authOptions =>
            {
                authOptions.ClientId = clientId;
                authOptions.ClientSecret = clientSecret;
                authOptions.Authority = authority;
                authOptions.Scope = scope;
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    authOptions.BaseUrl = baseUrl;
                }
            },
            configOptions =>
            {
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    configOptions.BaseUrl = baseUrl;
                }
            });
    }

    /// <summary>
    /// Adds the SeldonVigil SDK with OAuth2 authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientId">The client ID.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="authority">The OAuth2 authority URL.</param>
    /// <param name="scope">The OAuth2 scope (optional).</param>
    /// <param name="baseUrl">The base URL (optional).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSeldonVigilSdkWithOAuth2(
        this IServiceCollection services,
        string clientId,
        string clientSecret,
        Uri authority,
        string? scope = null,
        Uri? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(authority);

        return services.AddSeldonVigilSdkWithOAuth2(clientId, clientSecret, authority.ToString(), scope, baseUrl?.ToString());
    }

    /// <summary>
    /// Adds the SeldonVigil SDK with automatic trace export to SeldonVigil.
    /// </summary>
    /// <remarks>
    /// Registers an OpenTelemetry TracerProvider that pipes the application's
    /// System.Diagnostics.Activity spans through <see cref="SeldonVigilTraceProcessor"/>
    /// (tag enrichment) and <see cref="SeldonVigilTraceExporter"/> (delivery to
    /// SeldonVigil's trace ingestion endpoint).
    /// </remarks>
    public static IServiceCollection AddSeldonVigilSdkWithTraceExport(
        this IServiceCollection services,
        Action<SeldonVigilAuthenticationOptions>? configureAuthOptions = null,
        Action<SeldonVigilConfigurationOptions>? configureConfigOptions = null)
    {
        services.AddSeldonVigilSdk(configureAuthOptions, configureConfigOptions);

        services.AddSingleton<SeldonVigilTraceProcessor>();
        services.AddSingleton<SeldonVigilTraceExporter>();

        _ = services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSeldonVigilTracing());

        return services;
    }

    private static void AddSeldonVigilTracing(
        this TracerProviderBuilder builder)
    {
        _ = builder
            .AddProcessor(sp => sp.GetRequiredService<SeldonVigilTraceProcessor>())
            .AddProcessor(sp => new SeldonVigilSimpleExportProcessor(
                sp.GetRequiredService<SeldonVigilTraceExporter>()));
    }

    /// <summary>
    /// Bridges <see cref="SeldonVigilTraceExporter"/> into the processor pipeline
    /// (OTel exporters are not themselves processors).
    /// </summary>
    internal sealed class SeldonVigilSimpleExportProcessor(SeldonVigilTraceExporter exporter)
        : global::OpenTelemetry.BaseProcessor<System.Diagnostics.Activity>
    {
        public override void OnEnd(System.Diagnostics.Activity data)
        {
            if (data == null) return;
            var batch = new global::OpenTelemetry.Batch<System.Diagnostics.Activity>(data);
            _ = exporter.Export(in batch);
        }
    }
}
