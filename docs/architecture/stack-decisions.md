# SeldonVigil Stack Decision Log

Status: **Locked** — decisions below are architectural commitments. Changes after this
point require explicit review because downstream code, deployments and documentation
build on them.

Last reviewed: 2026-11-10

---

## 1. Primary Database — PostgreSQL (locked)

**Decision:** PostgreSQL is the sole OLTP store.

| Considered | Verdict | Reasoning |
|---|---|---|
| SQL Server | Rejected | Licensing cost breaks the self-hosted tier |
| MySQL/MariaDB | Rejected | No pgvector/Timescale ecosystem; weaker JSONB |
| CockroachDB / YugabyteDB | Rejected | Distributed-SQL solves scale we don't have; heavy per-install footprint |

Postgres is also the strategic hedge: it hosts pgvector (§3), can host TimescaleDB (§2),
and its LISTEN/NOTIFY + outbox pattern provides the lightweight event transport for the
single-box deployment profile (§7).

## 2. Trace Analytics — Partitioned Postgres → TimescaleDB when earned

**Decision:** No ClickHouse. No Apache Doris.

The previous infrastructure provisioned ClickHouse (Helm dependency, configmap,
network policy, alert rules) but **no application code ever read from or wrote to it**.
All ClickHouse infrastructure has been removed.

| Option | Verdict | Reasoning |
|---|---|---|
| ClickHouse | **Removed** | Extra JVM-free but ops-heavy service carried with zero code paths; unjustifiable until a tenant exceeds ~100M traces |
| Apache Doris | **Rejected** | Same category of problem as ClickHouse (distributed Java FE + BE nodes). Only wins on join-heavy petabyte BI workloads — not our shape |
| **TimescaleDB** | **Adopted when earned** | Postgres extension: same instance, same backups, same Npgsql/EF drivers. Continuous aggregates cover dashboards; native compression ~90%+ |
| Plain partitioned Postgres | **Current default** | Native range partitioning on `traces (start_time)` is sufficient well into hundreds of millions of rows |

Escalation path: `traces` grows past ~100M rows or dashboard queries degrade →
enable TimescaleDB extension and `create_hypertable('traces', 'StartTime')`. No client
code changes.

## 3. Vector Store — pgvector (only)

**Decision:** pgvector on the primary Postgres is the sole vector backend.
The Qdrant code path was removed (2026-11-10): no deployment ever provisioned it,
so it was a package, a config gate and a store implementation with no service
behind them.

Implementation: `IVectorStore` abstraction (`Shared/Infrastructure/VectorSearch/`),
with `PgVectorStore` as the only registered implementation.

| Backend | Selection | Notes |
|---|---|---|
| **PgVectorStore** | Always | Table `vector_memories`, HNSW cosine index, dimension via `Vectors:Dimensions` (default 3072 = text-embedding-3-large). Requires the `vector` extension installed on the server; fails fast with instructions if missing |

Rationale: one less service in every deployment; backups/tenancy/pooling inherited
from Postgres. If vector load ever needs isolating from OLTP, reintroduce a
dedicated store behind this same interface.

## 4. Distributed Cache — In-memory (default), Garnet/Redis (optional)

**Decision:** The app no longer requires Redis at startup.

```jsonc
// appsettings
{ "Cache": { "Provider": "Memory" } }   // default — process-local
{ "Cache": { "Provider": "Redis" } }    // any RESP-compatible server
```

| Backend | Verdict | Reasoning |
|---|---|---|
| In-process memory | **Default** | Agent-status cache doesn't justify a mandatory server for dev/small installs |
| **Garnet** | **Recommended external option** | Microsoft's .NET RESP-compatible server; drop-in for the StackExchange client; same-language operational story |
| Valkey | Acceptable alternative | BSD-licensed Redis fork if Garnet doesn't fit |
| Redis | Works unchanged | Fully compatible; keep existing instances |

Docker compose does not ship a cache server; the app defaults to in-process memory.
Any RESP-compatible server (Garnet/Valkey/Redis) can be run alongside for
multi-replica deployments. Connection string remains `ConnectionStrings__Redis`
regardless of server choice.

Caveat: multi-replica deployments **must** set `Cache:Provider=Redis`
(process memory is not shared).

## 5. Event Transport — Redpanda (cloud profile); Postgres bus planned

**Decision:** Redpanda is the event broker for all profiles. A lighter transport for the
single-box profile is planned (see §7) rather than immediate.

Redpanda is Kafka wire-compatible — the `Confluent.Kafka` client stack is unchanged.
Configuration uses the same `ConnectionStrings:Broker` key; TLS/SASL settings use the
`Broker` config section.

Retained as-is: event-class-per-topic naming (`TraceIngestedEvent`),
DLQ suffix `.dlq`, consumer groups `seldonvigil-trace-processor`,
`seldonvigil-security-analyzer`.

## 6. Runtime & Language — .NET 10 modular monolith (locked)

Correct for the target market (.NET enterprise agent governance).

### Resolved follow-ups

| Item | Resolution |
|---|---|
| Pin `LangVersion` to stable; disable `allowPrerelease` | ✅ `LangVersion=13`, `allowPrerelease: false` |
| Move migrations off startup path | ✅ `--migrate` CLI arg for init containers; Production defaults OFF, Development defaults ON |
| Wire persistence for Governance/Analytics/AIReasoning/Integration | ✅ `AppDataDbContext` with PG-backed repos replacing all five in-memory stores; app_* tables via idempotent DDL at startup |
| Enforce tenant scoping via EF global query filters sourced from JWT claims | ✅ `ITenantContext` + `HttpTenantContext` (JWT claim first); global `HasQueryFilter` on all tenant-owned entities across all four core DbContexts; `TenantScope.Begin()` ambient override for Workers |
| Persist Prompt/Evaluation repositories | ✅ `app_prompt_versions` + `app_evaluation_results` with PG-backed repositories; model config and metric scores stored as jsonb |

Remaining deferred items are tracked below and in the session notes.

### Still open (deferred)

| Item | Why deferred |
|---|---|
| Postgres-bus single-box deployment profile | Needs an `IEventBus` transport abstraction design; Redpanda works fine today |
| OTLP-native trace ingestion in the SDK | Requires protobuf endpoint in Observability module; current JSON API is functional |

## 7. Deployment Profile

One binary set, one deployment path: docker-compose.

| Concern | Choice |
|---|---|
| Cache | In-process memory (`Cache:Provider=Memory`); Redis optional for multi-replica |
| Vectors | pgvector |
| Trace analytics | Partitioned Postgres; TimescaleDB hypertables when earned |
| Event bus | Redpanda (single node in compose; self-managed cluster for production) |
| Migrations | Auto-apply at startup (Development); `--migrate` for production |
| Workflows | Timer workers + advisory locks |

## 8. Removed / Superseded

| Item | Status |
|---|---|
| ClickHouse Helm dependency, configmap entry, network-policy egress, replication-lag alert | **Deleted** (2026-08-24) — never wired to application code |
| `RedisCacheService` | Renamed `DistributedCacheService`; backend chosen by `Cache:Provider` |
| Qdrant sections in all appsettings files | Removed — pgvector is default; Qdrant documented as opt-in alternative |
| Qdrant code path (`Qdrant.Client`, `QdrantVectorStore`, `misc/qdrant_setup.py`) | **Removed** (2026-11-10) — no deployment ever provisioned Qdrant; pgvector is the only backend |
| `Seq` service + `Serilog.Sinks.Seq` config path | **Removed** (2026-11-10) — no `Seq:ServerUrl` in any environment; logs are console-only |
| `Grafana` service + empty provisioning dir | **Removed** (2026-11-10) — dashboard JSON kept as an optional artifact |
| `Jaeger` service + default `OpenTelemetry:OtlpEndpoint` | **Removed** (2026-11-10) — traces/meters export stays available via any OTLP collector; no collector is shipped by default |
| Kubernetes manifests, Helm chart, Terraform modules, AKS CD workflows | **Removed** (2026-11-10) — never completed: no chart templates, no terraform root module, CD referenced missing values files and scripts. docker-compose is the only deployment path |
| `Garnet` from default compose | **Removed** (2026-11-10) — app defaults to in-memory cache; Redis optional for multi-replica |
| `infrastructure/clickhouse/storage.xml`, `infrastructure/redis/redis-cluster-notes.yaml` | **Deleted** (2026-11-10) — orphans from removed topologies |
| k8s `Otlp__Endpoint` otel-collector reference | **Removed** (2026-11-10) — no collector manifest exists; set the key to an OTLP collector to enable export |
| `infrastructure/docker/prometheus/prometheus.yml/` (empty dir mounted as the Prometheus config file) | **Deleted** (2026-11-10) — compose now mounts `infrastructure/prometheus/prometheus.yml` |
| Five in-memory module repositories | **Replaced** (2026-08-25) — `AppDataDbContext` with PG-backed repositories for governance policies, certification pipelines, root-cause analyses, notifications, integrations |
| Startup migration race condition | **Resolved** (2026-08-25) — `--migrate` CLI arg + env-aware `MigrateOnStartup` default (Development ON, Production/Staging OFF) |
| API-key unparsable roles defaulted to Admin | **Fixed** (2026-08-25) — now falls back to Viewer (least privilege) |
| Five Broker management endpoints `.AllowAnonymous()` | **Fixed** (2026-08-25) — group-level `RequireAuthorization()` |
| CORS `AllowAnyOrigin()` | **Fixed** (2026-08-25) — `Cors:AllowedOrigins` allowlist; empty = deny all cross-origin |
| `LangVersion=preview` + `allowPrerelease: true` | **Pinned** (2026-08-25) — `LangVersion=13`, `allowPrerelease: false`; fixed Money converter expression-tree issue exposed by C# 13 |
