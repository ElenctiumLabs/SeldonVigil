# Trace Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/observability/traces` | Ingest a trace (triggers cost recording + broker event) |
| GET | `/api/v1/observability/traces` | List — filter: agentId, status, from, to, page, pageSize, limit |
| GET | `/api/v1/observability/traces/{id}` | Get a trace by ID |
