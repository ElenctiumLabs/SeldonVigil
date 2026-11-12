# SharedKernel & Telemetry

## SeldonVigil.SharedKernel

Shared domain abstractions used by both Api and Workers:

- `IEventBus` — publish domain events
- `IEventHandler<T>` — handle domain events
- `IDateTimeProvider` / `SystemDateTimeProvider`
- `ICacheService`
- Broker infrastructure: `EventBus`, `BrokerConsumerFactory`, `JsonEventSerializer<T>`
- Tenancy: `AddSeldonVigilTenancy()`, `ITenantContext`, `TenantScope`

## SeldonVigil.Telemetry

Shared telemetry bootstrap library, also published as a NuGet package so external consumers can use the same observability setup.

```csharp
// Register in any host
builder.Services.AddSeldonVigilTelemetry(configuration, "Api");
// Sets: service.name = "seldonvigil.Api"
// Exports: OTel traces and meters via OTLP, metrics via Prometheus

// Register logging
builder.Host.UseSerilog((ctx, sp, cfg) =>
  cfg.ConfigureSeldonVigilLogging(ctx.Configuration, "Api")
     .ReadFrom.Services(sp));
```

## SeldonVigil.SourceGenerators

Roslyn compile-time source generators (endpoint registration, policy expressions, strongly-typed ids, JSON serialisation). Runs at build time; no runtime dependency.
