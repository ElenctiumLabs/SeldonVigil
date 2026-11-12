# Infrastructure Stack

| Tool | Role |
|---|---|
| Docker + Docker Compose | The only deployment path — local development and self-hosting |
| PostgreSQL 18 | Primary database, pgvector for vectors |
| Redpanda | Event broker |
| Prometheus | Metrics scraper (targets the API `/metrics` endpoint) |
| nginx | Serves the web UI and proxies `/api` to the API |
| GitHub Actions | CI: build, test, dependency audit, image builds |

> Kubernetes, Helm, Terraform and ACR were removed during the open-source slim-down. Docker Compose is the single deployment path.
