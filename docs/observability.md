# Observability — Common Telemetry Endpoint

All SeldonVigil services (the platform plus external consumers)
emit telemetry through one shared bootstrap: **`SeldonVigil.Telemetry`**
(source: `src/SeldonVigil.Telemetry/`).

## What flows where (current, local dev)

| Signal | Path | Endpoint |
|---|---|---|
| Metrics | OTel Prometheus exporter (pull) | **SeldonVigil API** `http://localhost:8080/metrics`; external consumers expose their own `/metrics` |
| Logs    | Serilog → console | local console output; no log server is shipped |

Prometheus runs as a container from `infrastructure/docker/docker-compose.yml`.
Set `OpenTelemetry:OtlpEndpoint` to export traces and meters to any OTLP collector;
no collector is shipped by default.

## Service naming

Every exporter sets resource `service.name = seldonvigil.{App}` via
`SeldonVigilTelemetryBuilder.AddSeldonVigilTelemetry(config, appName)`:

| App | service.name | Meter |
|---|---|---|
| SeldonVigil.Api | `seldonvigil.Api` | `SeldonVigil.Api` |
| SeldonVigil.Workers | `seldonvigil.Workers` | `SeldonVigil.Workers` |
| External app | `seldonvigil.{AppName}` | `SeldonVigil.{AppName}` |

Prometheus can slice by these labels.

## Configuration keys

```jsonc
{
  "OpenTelemetry": { "OtlpEndpoint": "http://localhost:4317" } // optional: enables traces+meters export to an OTLP collector
}
```

Absent keys = that signal disabled (all sinks degrade gracefully). No collector is
shipped in `docker-compose.yml`.

## Wiring a new service

1. Reference `SeldonVigil.Telemetry` (package for external solutions,
   project reference inside the SeldonVigil solution).
2. Web apps:
   ```csharp
   builder.Host.UseSerilog((ctx, sp, cfg) => cfg
       .ConfigureSeldonVigilLogging(ctx.Configuration, "MyApp")
       .ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(sp));
   builder.Services.AddSeldonVigilTelemetry(builder.Configuration, "MyApp");
   ```
3. Worker hosts use `builder.Services.AddSerilog((sp, cfg) => ...)` instead
   (`HostApplicationBuilder` has no `.Host`).
4. Expose `/metrics` via `AddOpenTelemetry().WithMetrics(m => m.AddPrometheusExporter())`
   + `app.MapPrometheusScrapingEndpoint("/metrics")`.
5. Add a scrape job in `infrastructure/prometheus/prometheus.yml`.

## Local run notes

- Start order: Redpanda → Prometheus → SeldonVigil.Api → Workers.
- Use `dotnet run --no-build` once binaries exist; `dotnet run` rebuilds and
  races file locks between concurrently-running projects.
- Redpanda is the event broker. The Broker API binds
  `127.0.0.1:9092` (host) / `redpanda:9092` (in-network). All appsettings
  use `127.0.0.1:9092` for local development.
- Consumer-lag metrics are scraped from Redpanda's `/public_metrics` endpoint
  (`redpanda_kafka_consumer_group_lag_sum`). See `docs/runbooks/redpanda.md`
  for operational commands.
