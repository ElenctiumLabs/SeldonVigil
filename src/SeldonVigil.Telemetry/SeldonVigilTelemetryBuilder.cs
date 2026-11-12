using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Globalization;

namespace SeldonVigil.Telemetry;

/// <summary>
/// Common observability bootstrap shared by all SeldonVigil services
/// (SeldonVigil platform and consuming solutions).
///
/// Logging  : Serilog -> console.
/// Tracing  : OpenTelemetry OTLP export when "OpenTelemetry:OtlpEndpoint" is set.
/// Meters   : One named meter per service ("SeldonVigil.&lt;App&gt;" or supplied name),
///            exported over OTLP; scrape endpoints are owned by each host app.
/// Resource : service.name = "seldonvigil.{appName}" on all exported telemetry.
/// </summary>
public static class SeldonVigilTelemetryBuilder
{
    /// <summary>Standard resource service name: "seldonvigil.{appName}".</summary>
    public static string ServiceName(string appName) => $"seldonvigil.{appName}";
    /// <summary>Standard meter naming: "SeldonVigil.{appName}".</summary>
    public static string MeterName(string appName) => $"SeldonVigil.{appName}";

    /// <summary>
    /// Configures a Serilog logger with the common sinks. Apply with
    /// <c>builder.Host.UseSerilog((ctx, sp, cfg) => cfg.ConfigureSeldonVigilLogging(ctx.Configuration, "MyApp").ReadFrom.Services(sp))</c>.
    /// </summary>
    public static LoggerConfiguration ConfigureSeldonVigilLogging(
        this LoggerConfiguration configuration,
        IConfiguration config,
        string appName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(config);

        _ = configuration
            .Enrich.WithProperty("service.name", appName)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:l}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture);

        // OTLP log export ships when a collector endpoint is present.
        var otlpLogs = config["OpenTelemetry:OtlpLogsEndpoint"];
        if (!string.IsNullOrEmpty(otlpLogs))
        {
            _ = configuration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpLogs;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = appName
                };
            });
        }

        return configuration.MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning);
    }

    /// <summary>
    /// Registers OpenTelemetry tracing + meters for the service. Inactive when
    /// "OpenTelemetry:OtlpEndpoint" is not configured.
    /// </summary>
    public static IServiceCollection AddSeldonVigilTelemetry(
        this IServiceCollection services,
        IConfiguration config,
        string appName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        var otlpEndpoint = config["OpenTelemetry:OtlpEndpoint"];
        if (string.IsNullOrEmpty(otlpEndpoint))
        {
            return services;
        }

        _ = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: ServiceName(appName), serviceNamespace: "seldonvigil"))
            .WithTracing(tracing =>
            {
                _ = tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(MeterName(appName))
                    .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                _ = metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(MeterName(appName))
                    .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            });

        return services;
    }
}
