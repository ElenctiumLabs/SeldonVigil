# SeldonVigil.Api

The primary HTTP server. Built with ASP.NET Core Minimal APIs. All business logic lives in MediatR handlers; endpoints are thin mappers from HTTP to commands/queries.

## Startup sequence (`Program.cs`)

1. Configure Serilog + OpenTelemetry
2. Register `JsonStringEnumConverter` globally (enums serialised as strings, never ints)
3. Load config: `appsettings.json` → env-specific → `appsettings.Local.json` → env vars
4. Register all modules, rate limiting, CORS, auth
5. Validate configuration on startup
6. Ensure the database schema (`Database:MigrateOnStartup`, default ON in Development)
7. Start listening

## Module registration

```csharp
builder.Services
  .AddSeldonVigilIdentityModule()
  .AddSeldonVigilSecurityModule()
  .AddSeldonVigilCostIntelligenceModule()
  .AddSeldonVigilObservabilityModule()
  .AddSeldonVigilDomain()
  .AddSeldonVigilSharedInfrastructure(config)
  .AddSeldonVigilAuthentication(config)
  .AddSeldonVigilAuthorization()
  .AddSeldonVigilCaching(config)
  .AddSeldonVigilMessaging(config)
  .AddSeldonVigilTelemetry(config);
```

## Persistence

A single consolidated `SeldonVigilDbContext` (PostgreSQL) maps every module's entities. The schema is created with idempotent DDL at startup from `Shared/Infrastructure/Configuration/ConfigureDatabase.cs`. EF Core migrations exist for the legacy per-module contexts but are not on the startup path.

## Key endpoints

- `GET /healthz` — liveness
- `GET /readyz` — readiness
- `GET /metrics` — Prometheus scrape (unauthenticated)
- `GET /swagger` — Swagger UI
- `GET /dev/token?role=Administrator` — dev-only JWT (gated by `DevAuth:Enabled` and the Development environment)
