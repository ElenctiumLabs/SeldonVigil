# System Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/healthz` | Liveness probe (unauthenticated) |
| GET | `/readyz` | Readiness probe (unauthenticated) |
| GET | `/metrics` | Prometheus scrape (unauthenticated) |
| GET | `/swagger` | Swagger UI |
| GET | `/api/v1/notifications` | List notifications |
| GET | `/api/v1/notifications/unread` | Unread notifications count |
| POST | `/api/v1/notifications/{id}/read` | Mark a notification as read |
