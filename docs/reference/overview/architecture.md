# Architecture

SeldonVigil is a **.NET 10 modular monolith**. A single deployable binary whose code is partitioned into independent modules, each owning its domain entities, MediatR handlers, and Minimal API endpoints.

> The open-source build consolidates persistence into a single `SeldonVigilDbContext` (PostgreSQL). The modules still own their own domain and application layers.

```mermaid
flowchart TD
    subgraph Clients
        A[React SPA]
        B[SeldonVigil.Sdk]
        C[SeldonVigil.Cli]
        D[Any HTTP client]
    end

    E[SeldonVigil.Api<br/>ASP.NET Core Minimal APIs]
    F[SeldonVigil.Workers<br/>hosted services]

    subgraph Stores
        G[(PostgreSQL<br/>primary OLTP)]
        H[In-memory cache]
        I[pgvector<br/>vector store]
        J[Redpanda<br/>event bus]
    end

    subgraph Telemetry
        K[Prometheus<br/>metrics]
        L[OTLP collector<br/>traces and meters]
    end

    A & B & C & D -->|HTTPS / JWT| E
    E -->|broker events| F
    F --> G & H & I & J
    E --> K
    E --> L
```

## Architectural patterns

| Pattern | Implementation |
|---|---|
| CQRS | Every action is a MediatR `ICommand` or `IQuery` |
| Domain-Driven Design | Each module: `Domain / Application / Infrastructure / Api` layers |
| Result pattern | Handlers return `Result<T>` — no exception-driven control flow |
| Event-driven | Domain events published to the broker, consumed by background workers |
| Minimal APIs | No MVC controllers — endpoint groups registered per module |
| Modular monolith | Strong module boundaries; persistence consolidated in one DbContext |

## Process boundary

| Process | Project | Role |
|---|---|---|
| API server | `src/SeldonVigil.Api` | HTTP request/response, JWT auth, module endpoints |
| Worker host | `src/SeldonVigil.Workers` | Broker consumers, periodic background jobs, no inbound HTTP |
