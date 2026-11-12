# Broker Topics & Consumer Groups

## Topics

| Topic | Event type | DLQ |
|---|---|---|
| `TraceIngestedEvent` | Published by the API on trace ingestion | `TraceIngestedEvent.dlq` |
| `TraceEnrichedEvent` | Published by TraceProcessorWorker after enrichment | `TraceEnrichedEvent.dlq` |

## Consumer groups

| Group | Worker | Topic |
|---|---|---|
| `seldonvigil-trace-processor` | TraceProcessorWorker | TraceIngestedEvent |
| `seldonvigil-security-analyzer` | SecurityAnalyzerWorker | TraceIngestedEvent |

**Redpanda** is the event broker. Single binary, no JVM.
