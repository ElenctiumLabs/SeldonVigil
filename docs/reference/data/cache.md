# Cache

The cache backend is selected via configuration — no code changes are required to switch.

```json
{ "Cache": { "Provider": "Memory" } }  // default — process-local
{ "Cache": { "Provider": "Redis" } }   // any RESP-compatible server
```

| Backend | Status | Notes |
|---|---|---|
| In-process memory | Default | Zero dependencies; not shared across replicas |
| Redis / Valkey | Optional | Any RESP-compatible server, for multi-replica deployments |

> Multi-replica deployments **must** set `Cache:Provider=Redis`. Process memory is not shared between replicas.
