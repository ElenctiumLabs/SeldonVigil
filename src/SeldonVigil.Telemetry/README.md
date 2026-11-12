# SeldonVigil Telemetry

Shared observability bootstrap (Serilog + OpenTelemetry) for SeldonVigil services
and consuming solutions.

## Features

- **Serilog console logging** with a consistent output template and `service.name` enrichment
- **OpenTelemetry tracing and metrics** exported over OTLP when an endpoint is configured
- **Conventional naming**: resource `service.name` = `seldonvigil.{appName}`, meter = `SeldonVigil.{appName}`
- **Zero configuration** when no OTLP endpoint is set - logging only, no telemetry pipeline

## Installation

```bash
dotnet add package SeldonVigil.Telemetry
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Telemetry;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Logging: Serilog with the shared sinks and enrichment.
builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ConfigureSeldonVigilLogging(context.Configuration, "MyApp")
        .ReadFrom.Services(services));

// Tracing + meters: active only when an OTLP endpoint is configured.
builder.Services.AddSeldonVigilTelemetry(builder.Configuration, "MyApp");
```

## Configuration

Both helpers read from `IConfiguration`, so the telemetry pipeline stays
inactive unless the relevant keys are present:

| Key | Effect |
| --- | --- |
| `OpenTelemetry:OtlpEndpoint` | Enables OTLP export of traces and metrics |
| `OpenTelemetry:OtlpLogsEndpoint` | Enables OTLP export of Serilog events |

Exported telemetry carries `service.name = seldonvigil.{appName}` and, for
traces, `service.namespace = seldonvigil`.

## Naming Helpers

```csharp
string resourceName = SeldonVigilTelemetryBuilder.ServiceName("MyApp"); // "seldonvigil.MyApp"
string meterName    = SeldonVigilTelemetryBuilder.MeterName("MyApp");    // "SeldonVigil.MyApp"
```

## License

MIT License - see LICENSE file for details.

## Support

For issues and questions, please visit our GitHub repository.
