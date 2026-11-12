# SeldonVigil Hosting Requirements

**Last Updated**: November 2026
**Purpose**: Deployment specifications for self-hosting SeldonVigil

> For local testing on Windows, see the [WSL2 + Docker Environment](development/wsl2-docker-environment.md) runbook.

> **WARNING — the default compose stack runs in Development mode.**
> It enables the `/dev/token` endpoint and the development login backdoor (any
> username with password `dev` returns an administrator token). Never expose it to
> an untrusted network. Production deployments must apply
> `docker-compose.prod.yml` and the settings under
> [Production configuration](#production-configuration).

---

## Overview

SeldonVigil is a .NET 10 modular monolith for AI agent governance. The deployment
path is Docker Compose: the API, background workers, Redpanda for events, and
Prometheus for metrics. PostgreSQL runs on the host.

---

## Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| **Runtime** | .NET 10 | 10.0 |
| **API** | ASP.NET Core minimal APIs | 10.0 |
| **Database** | PostgreSQL + pgvector | 15+ |
| **Cache** | In-memory (default) / Garnet or Redis (optional) | Latest |
| **Event Broker** | Redpanda | v26.2.2 |
| **Vector Store** | pgvector | - |
| **Trace Analytics** | Partitioned Postgres → TimescaleDB (earned) | - |
| **Observability** | Prometheus | Latest |
| **Container** | Distroless (`mcr.microsoft.com/dotnet/aspnet:10.0`) | - |

---

## Docker Compose

### Services
```yaml
services:
  api:           # SeldonVigil API (port 8080)
  worker:        # Background workers (2 replicas)
  redpanda:      # Event broker (port 9092)
  redpanda-setup:# One-shot consumer-lag metrics config
  prometheus:    # Metrics (port 9090)
  web:           # Web UI (port 80)
```

### External Dependencies (Host-Managed)
| Service | Host | Port | Notes |
|---------|------|------|-------|
| **PostgreSQL** | Host (localhost) | 5432 | Must have the `vector` extension; database `seldonvigil` |

### Environment Variables
Copy `infrastructure/docker/.env.example` to `.env` and adjust. The example is
committed; `.env` holds the real values and is gitignored.

### Ports
| Port | Service | Access |
|------|---------|--------|
| 80 | Web UI | Public (via reverse proxy) |
| 8080 | API HTTP | Public (via reverse proxy) |
| 9092 | Redpanda broker API | Internal |
| 9644 | Redpanda Admin | Internal |
| 9090 | Prometheus | Internal |

### Resource Requirements
| Resource | Minimum | Recommended |
|----------|---------|-------------|
| **CPU** | 2 vCPU | 4 vCPU |
| **RAM** | 8 GB | 16 GB |
| **Storage** | 100 GB SSD | 500 GB SSD |
| **Network** | 1 Gbps | 10 Gbps |

---

## Production Configuration

The base `docker-compose.yml` is a **development** configuration. For production,
apply the production overrides:

```bash
cd infrastructure/docker
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

The overrides set `ASPNETCORE_ENVIRONMENT=Production` and
`DOTNET_ENVIRONMENT=Production`, which disables the development token endpoint and
the development login backdoor, and turns on the production configuration checks.

Required environment variables, in addition to `POSTGRES_PASSWORD`:

| Variable | Purpose |
|----------|---------|
| `AUTHORITY` | OIDC discovery URL of the real identity provider |
| `AUDIENCE` | API audience issued by that provider |
| `BROKER_ADDRESS` | Broker bootstrap servers (for example `redpanda.example.com:9093`) |
| `BROKER_SECURITY_PROTOCOL` | `SASL_SSL` |
| `BROKER_SASL_MECHANISM` | `SCRAM-SHA-256` |
| `BROKER_SASL_USERNAME` | Broker principal |
| `BROKER_SASL_PASSWORD` | Broker principal password |
| `BROKER_SSL_CA_LOCATION` | Path to the broker CA certificate |

Optional:

| Variable | Purpose |
|----------|---------|
| `AZURE_KEYVAULT_NAME` | Set to load secrets from Azure Key Vault; when unset, secrets come from environment variables |
| `REDIS_CONNECTION_STRING` | Set to a shared cache for multi-replica deployments (also set `Cache:Provider=Redis`) |

The API refuses to start when a required value is missing, so a misconfigured
production deployment fails closed instead of silently running in development mode.

---

## Database Requirements

### PostgreSQL (Required)
| Requirement | Specification |
|-------------|---------------|
| **Version** | 15+ |
| **Extensions** | `vector` (pgvector), `uuid-ossp` |
| **Collation** | `en_US.UTF-8` |
| **Max Connections** | 200+ (per pool) |
| **Shared Buffers** | 25% RAM |
| **Effective Cache** | 75% RAM |

### Schemas (Auto-Managed via EF Migrations)
- `public` — Core tables (tenants, users, agents, traces, etc.)
- `app_governance` — Governance policies, certifications
- `app_analytics` — Analytics, root-cause analyses
- `app_ai_reasoning` — AI reasoning traces
- `app_integrations` — Third-party integrations

### Connection String
```bash
Host=host.docker.internal;Port=5432;Database=seldonvigil;Username=postgres;Password=${POSTGRES_PASSWORD}
```

---

## Cache Requirements

### Default: In-Memory
- **Configuration**: `Cache:Provider=Memory`
- **Use Case**: Single replica, dev, small deployments
- **No external dependency**

### Optional: Garnet/Redis (multi-replica)
- **Image**: `ghcr.io/microsoft/garnet:latest` (or Valkey/Redis)
- **Port**: 6379 (RESP protocol)
- **Persistence**: AOF + RDB
- **Configuration**: `Cache:Provider=Redis`, `ConnectionStrings__Redis=<host>:6379`
- **Note**: not shipped in `docker-compose.yml`; run it yourself when running more than one API replica.

---

## Event Broker Requirements

### Redpanda (Required)
| Setting | Value |
|---------|-------|
| **Image** | `docker.redpanda.com/redpandadata/redpanda:v26.2.2` |
| **Mode** | `dev-container` (single) / Cluster (prod) |
| **Broker API** | Port 9092 (internal), 19092 (external) |
| **Admin API** | Port 9644 |
| **Consumer Group Metrics** | Enabled via `redpanda-setup` job |

### Topics (Auto-Created)
- `trace-ingested` — Trace ingestion events
- `security-events` — Security analysis events
- `*.dlq` — Dead letter queues

---

## Observability Stack

| Component | Image | Ports | Purpose |
|-----------|-------|-------|---------|
| **Prometheus** | `prom/prometheus:v2.53.0` | 9090 | Metrics collection |

An optional dashboard JSON lives at `infrastructure/grafana/seldonvigil-dashboard.json`
for anyone who runs Grafana alongside Prometheus.

---

## Deployment Checklist

### Pre-Deployment
- [ ] PostgreSQL 15+ with the `vector` extension installed
- [ ] Database `seldonvigil` created with user `seldonvigil`
- [ ] DNS records configured (A/AAAA → VPS IP)
- [ ] SSL certificates (Let's Encrypt via Caddy/nginx)
- [ ] Environment variables in `.env` (never commit)
- [ ] Backup strategy defined

### Deploy
```bash
# 1. Clone repository
git clone <repo> && cd SeldonVigil

# 2. Configure environment
cp infrastructure/docker/.env.example infrastructure/docker/.env
# Edit .env with secure passwords

# 3. Start PostgreSQL on host (not in compose)
sudo apt install postgresql-15 postgresql-15-pgvector
sudo -u postgres psql -c "CREATE DATABASE seldonvigil;"
sudo -u postgres psql -c "CREATE USER seldonvigil WITH ENCRYPTED PASSWORD '...';"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE seldonvigil TO seldonvigil;"
sudo -u postgres psql -d seldonvigil -c "CREATE EXTENSION vector;"

# 4. Deploy
cd infrastructure/docker
docker-compose up -d

# 5. Verify
curl http://localhost          # Web UI
curl http://localhost:8080/healthz
curl http://localhost:9090  # Prometheus
```

### Production Hardening
- [ ] Reverse proxy (Caddy/nginx) with TLS termination
- [ ] Firewall: Only 80/443 public, rest internal/VPN
- [ ] Prometheus behind auth or VPN
- [ ] Daily PostgreSQL backups (pg_dump to off-site)
- [ ] Volume snapshots for Redpanda/Prometheus
- [ ] Log rotation configured
- [ ] Alert routing for the Prometheus rules (Alertmanager or equivalent)

---

## Quick Reference

### Start
```bash
cd infrastructure/docker
docker-compose up -d
```

### View Logs
```bash
docker-compose logs -f api
docker-compose logs -f worker
```

### Web UI
Open http://localhost in a browser. The UI is served by nginx and proxies
`/api` to the API.

### Health Checks
```bash
curl http://localhost:8080/healthz
```

### Scale Workers
```bash
docker-compose up -d --scale worker=4
```

### Backup Database
```bash
pg_dump -h localhost -U seldonvigil -d seldonvigil > backup_$(date +%F).sql
```

---

## Runbooks

- **Redpanda operations**: `docs/runbooks/redpanda.md`
- **Architecture decisions**: `docs/architecture/stack-decisions.md`
