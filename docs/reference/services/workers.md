# SeldonVigil.Workers

A .NET Generic Host process that runs entirely as background services — no inbound HTTP. Consumes broker topics and runs periodic timers.

## Hosted services

| Worker | Trigger | Role |
|---|---|---|
| `TraceProcessorWorker` | `TraceIngestedEvent` (group `seldonvigil-trace-processor`) | Enriches ingested traces |
| `SecurityAnalyzerWorker` | `TraceIngestedEvent` (group `seldonvigil-security-analyzer`) | Runs security policy evaluation on every trace |
| `CostOptimizerWorker` | Periodic timer | Analyses spend, generates cost optimisation recommendations |
| `CertificationPipelineWorker` | Periodic timer | Advances certification pipelines |
| `PolicyDriftDetectorWorker` | Periodic timer | Detects agent policy drift |
| `AuditLogArchiverWorker` | Periodic timer | Archives and rotates audit logs |
| `ComplianceScannerWorker` | Periodic timer | Runs compliance scans across tenants |
| `ReportGeneratorWorker` | Periodic timer | Generates scheduled reports |
| `DataRetentionManagerWorker` | Periodic timer | Enforces data retention policies |
| `BrokerSubscriptionBootstrapper` | Startup | Subscribes workers to their broker topics |

> **Broker on Windows:** the worker normalises `localhost` → `127.0.0.1` at startup. On Windows, `localhost` resolves to `::1` (IPv6) first; locally installed brokers typically bind IPv4 only, causing intermittent "all brokers down" failures with librdkafka.
