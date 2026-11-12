# Backend Stack

| Layer | Technology | Notes |
|---|---|---|
| Language / Runtime | C# / .NET 10 | Locked |
| Web framework | ASP.NET Core Minimal APIs | No MVC controllers |
| ORM | Entity Framework Core | Schema ensured on startup (configurable) |
| Mediator | MediatR 12 | Apache-2.0 licensed |
| Primary database | PostgreSQL 18 | Sole OLTP store; locked |
| Cache | In-memory (default) / Redis-compatible (optional) | Switch via `Cache:Provider` |
| Vector store | pgvector | Requires the `vector` extension |
| Event bus | Redpanda | Single binary, no JVM |
| Distributed tracing | OpenTelemetry OTLP | Export to any OTLP collector |
| Metrics | OpenTelemetry Prometheus exporter | Scraped at `/metrics` |
| Structured logging | Serilog → console | Service name: `seldonvigil.Api`, `seldonvigil.Workers` |
| Secret management | Environment variables | Azure Key Vault optional |
| AI / LLM | OpenAI API | Used by the AIReasoning module; key via env |
| Rate limiting | ASP.NET Core built-in | `AddSeldonVigilRateLimiting` |
| Resilience | Polly | Circuit breakers and retries for outbound calls |
