using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeldonVigil.Api.Modules.CostIntelligence;
using SeldonVigil.Api.Modules.Governance.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Observability;
using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.Api.Modules.Security;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using SeldonVigil.SharedKernel.Infrastructure;
using SeldonVigil.SharedKernel.Infrastructure.Messaging;
using SeldonVigil.Workers.Workers;
using SeldonVigil.Telemetry;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Logging — shared observability bootstrap (console)
builder.Services.AddSerilog((services, cfg) => cfg
    .ConfigureSeldonVigilLogging(builder.Configuration, "Workers")
    .ReadFrom.Services(services));
builder.Services.AddSeldonVigilTelemetry(builder.Configuration, "Workers");

// Logging
builder.Logging.AddConsole();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    _ = cfg.RegisterServicesFromAssemblyContaining<Program>();
});

// Broker
builder.Services.Configure<BrokerConsumerOptions>(
    builder.Configuration.GetSection(BrokerConsumerOptions.SectionName));
builder.Services.Configure<BrokerSecurityOptions>(
    builder.Configuration.GetSection(BrokerSecurityOptions.SectionName));

// Normalize localhost -> IPv4 loopback: on Windows, "localhost" resolves to ::1
// first while locally installed brokers commonly bind IPv4 only, which makes
// librdkafka connections intermittently fail with "all brokers down".
var brokerBootstrap = BrokerBootstrapServers.Normalize(
    builder.Configuration.GetConnectionString("Broker") ?? throw new InvalidOperationException());

builder.Services.AddSingleton<IBrokerConsumerFactory>(sp =>
    new BrokerConsumerFactory(brokerBootstrap,
        sp.GetRequiredService<IOptions<BrokerSecurityOptions>>().Value));

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = BrokerClientSecurity.Apply(new ProducerConfig
    {
        BootstrapServers = brokerBootstrap,
        ClientId = "seldonvigil-workers",
        CompressionType = CompressionType.Lz4,
        LingerMs = 5
    }, sp.GetRequiredService<IOptions<BrokerSecurityOptions>>().Value);
    return new ProducerBuilder<string, string>(config).Build();
});

builder.Services.AddSingleton<IEventBus, EventBus>();

// Typed consumers (one per consumer group so workers don't fight over partitions)
builder.Services.AddKeyedSingleton<IConsumer<string, TraceIngestedEvent>>(
    "trace-processor",
    (sp, _) =>
    {
        var factory = sp.GetRequiredService<IBrokerConsumerFactory>();
        var consumer = factory.CreateConsumer<TraceIngestedEvent>(
            nameof(TraceIngestedEvent),
            "seldonvigil-trace-processor",
            new JsonEventSerializer<TraceIngestedEvent>());
        consumer.Subscribe(nameof(TraceIngestedEvent));
        return consumer;
    });

builder.Services.AddKeyedSingleton<IConsumer<string, TraceIngestedEvent>>(
    "security-analyzer",
    (sp, _) =>
    {
        var factory = sp.GetRequiredService<IBrokerConsumerFactory>();
        var consumer = factory.CreateConsumer<TraceIngestedEvent>(
            nameof(TraceIngestedEvent),
            "seldonvigil-security-analyzer",
            new JsonEventSerializer<TraceIngestedEvent>());
        consumer.Subscribe(nameof(TraceIngestedEvent));
        return consumer;
    });

    // Persistence needed by workers.
    // AddSeldonVigilTenancy first: tenant-scoped DbContexts inject ITenantContext;
    // workers resolve tenants from the ambient TenantScope set per message.
    builder.Services.AddSeldonVigilTenancy();

    // Consolidated DbContext for all modules
    builder.Services.AddDbContextPool<SeldonVigil.Api.Shared.Infrastructure.Persistence.SeldonVigilDbContext>((sp, options) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("SeldonVigil.Api");
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });
        options.EnableSensitiveDataLogging(false);
    });

    builder.Services.AddSeldonVigilSecurityModule(builder.Configuration);
    builder.Services.AddSeldonVigilCostIntelligenceModule(builder.Configuration);
    builder.Services.AddSeldonVigilObservabilityModule(builder.Configuration);

    // Governance + Identity repositories needed by the certification worker.
    // CertificationPipeline itself is cross-tenant (no query filter); Agent and
    // SecurityViolation are tenant-filtered, so the worker sets TenantScope per pipeline.
    builder.Services.AddScoped<CertificationPipelineRepository>();
    builder.Services.AddScoped<IAgentRepository, AgentRepository>();

// Shared
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// Event handlers for broker subscriptions
builder.Services.AddScoped<TraceEnrichedEventHandler>();
builder.Services.AddScoped<IEventHandler<TraceEnrichedEvent>, TraceEnrichedEventHandler>();

// Hosted services
builder.Services.AddHostedService<TraceProcessorWorker>();
builder.Services.AddHostedService<CostOptimizerWorker>();
builder.Services.AddHostedService<SecurityAnalyzerWorker>();
builder.Services.AddHostedService<CertificationPipelineWorker>();
builder.Services.AddHostedService<PolicyDriftDetectorWorker>();
builder.Services.AddHostedService<AuditLogArchiverWorker>();
builder.Services.AddHostedService<ComplianceScannerWorker>();
builder.Services.AddHostedService<ReportGeneratorWorker>();
builder.Services.AddHostedService<DataRetentionManagerWorker>();
builder.Services.AddHostedService<BrokerSubscriptionBootstrapper>();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);