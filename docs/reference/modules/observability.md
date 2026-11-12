# Observability Module

The core data-collection module. Receives traces from agents, stores them, and triggers downstream processing.

## Ingestion flow

1. Client calls `POST /api/v1/observability/traces`
2. `IngestTraceCommand` validates the agent is Active
3. The trace is persisted to `SeldonVigilDbContext`
4. Cost is recorded inline (best-effort) against the agent's budget
5. `TraceIngestedEvent` is published to the broker
6. Workers consume the event asynchronously for enrichment and security analysis

## Trace schema (key fields)

```json
{
  "agentId": "uuid",
  "operationName": "string",
  "operationType": "LlmCall | Custom | ...",
  "status": "Ok | Error | Timeout",
  "startTime": "ISO8601",
  "endTime": "ISO8601",
  "inputTokens": 0,
  "outputTokens": 0,
  "cachedTokens": 0,
  "cost": 0.0,
  "attributes": { "key": "value" },
  "events": [],
  "links": []
}
```
