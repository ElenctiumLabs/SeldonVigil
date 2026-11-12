using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SeldonVigil.Api.Shared.Infrastructure.HealthChecks;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.SharedKernel.Infrastructure.Messaging;

namespace SeldonVigil.Api.Shared.Infrastructure.Configuration;

public static class ConfigureHealthChecks
{
    public static void AddSeldonVigilHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var brokerBootstrap = BrokerBootstrapServers.Normalize(
            configuration.GetConnectionString("Broker") ?? "localhost:9092");
        var brokerSecurity = configuration
            .GetSection(BrokerSecurityOptions.SectionName)
            .Get<BrokerSecurityOptions>();

        services.AddHealthChecks()
            .AddDbContextCheck<SeldonVigilDbContext>("postgresql")
            .AddCheck<AuthenticationHealthCheck>("authentication")
            .AddKafka(
                BrokerClientSecurity.Apply(new ProducerConfig
                {
                    BootstrapServers = brokerBootstrap,
                    // Bound the check: with a down broker, ProduceAsync otherwise waits
                    // on librdkafka's default message.timeout.ms (300s) and /healthz hangs.
                    MessageTimeoutMs = 5000,
                    SocketTimeoutMs = 3000,
                    RequestTimeoutMs = 3000
                }, brokerSecurity),
                name: "broker");
    }

    public static void MapSeldonVigilHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });
        app.MapHealthChecks("/readyz", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });
    }
}
