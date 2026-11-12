# Implementation Roadmap

SeldonVigil is feature-complete for the open-source release. Remaining work is tracked in `docs/architecture/stack-decisions.md`:

| Priority | Area | Status |
|---|---|---|
| P0 | Correctness and security | Complete — tests, saga compensation, tenant scoping, dev-auth gating |
| P1 | Persistence | Complete — all module stores are PostgreSQL-backed |
| P2 | Deployment profiles | Compose only; a Postgres-bus single-box profile is deferred |
| P3 | Future | OTLP-native trace ingestion, TimescaleDB at scale, CLI completeness |
