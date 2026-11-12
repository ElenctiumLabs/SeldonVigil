# Architecture Decisions

Status: **Locked**. Source: `docs/architecture/stack-decisions.md`.

## ADR-1: PostgreSQL as sole OLTP store

**Decision:** PostgreSQL is the only relational database supported. SQL Server (licensing), MySQL (no pgvector/JSONB ecosystem), and distributed-SQL variants (CockroachDB, YugabyteDB) were rejected.

**Rationale:** PostgreSQL is the strategic hedge — it hosts pgvector, can host TimescaleDB, and its LISTEN/NOTIFY + outbox pattern provides lightweight event transport for single-box deployments.

## ADR-2: No ClickHouse — TimescaleDB when earned

**Decision:** ClickHouse was removed during the slim-down; no application code reads from or writes to it.

**Escalation path:** when `traces` grows past ~100M rows, enable the TimescaleDB extension and `create_hypertable`. No client code changes are needed.

## ADR-3: pgvector for vector search

**Decision:** `PgVectorStore` is the only vector backend. It stores embeddings in PostgreSQL with an HNSW cosine index — no extra service required.

## ADR-4: In-process memory cache by default

**Decision:** `Cache:Provider=Memory` is the default. For multi-replica deployments, switch to any RESP-compatible server (Redis/Valkey).

## ADR-5: Redpanda for the event bus

**Decision:** Redpanda is retained (single binary, no JVM). A lighter transport (Postgres LISTEN/NOTIFY outbox) is planned for the single-box profile.

## ADR-6: .NET 10 modular monolith

**Decision:** Locked. C# / .NET 10 fits the target market. The modular monolith provides strong domain boundaries without the operational complexity of microservices.

## ADR-7: Docker Compose is the only deployment path

**Decision:** Kubernetes, Helm and Terraform were removed during the open-source slim-down. Docker Compose is the single deployment path.
