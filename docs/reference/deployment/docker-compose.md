# Docker Compose

File: `infrastructure/docker/docker-compose.yml`

PostgreSQL runs on the host (`localhost:5432`); the API and workers reach it via `host.docker.internal`. Redpanda is containerised.

| Service | Description | Port(s) |
|---|---|---|
| `web` | nginx serving the React SPA and the docs, proxying `/api` to the API | 80 |
| `api` | SeldonVigil.Api | 8080 |
| `worker` | SeldonVigil.Workers (2 replicas) | — |
| `redpanda` | Event broker | 9092 (broker), 9644 (admin) |
| `redpanda-setup` | One-shot: enables consumer-group lag metrics | — |
| `prometheus` | Metrics scraper | 9090 |

See `docs/development/wsl2-docker-environment.md` for a complete walkthrough.
