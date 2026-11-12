# PostgreSQL

The sole OLTP store. This is a locked architecture decision — no other relational database is supported.

## Schema

A single consolidated `SeldonVigilDbContext` maps every module's entities. Tables are created with idempotent DDL at startup (`Shared/Infrastructure/Configuration/ConfigureDatabase.cs`); EF Core migrations exist for the legacy per-module contexts but are not on the startup path.

| Table | Owner |
|---|---|
| `agents`, `users`, `user_sessions` | Identity |
| `security_policies`, `security_violations` | Security |
| `traces` | Observability |
| `cost_budgets` | CostIntelligence |
| `app_governance_policies`, `app_certification_pipelines` | Governance |
| `app_root_cause_analyses` | AIReasoning |
| `app_notifications` | Analytics |
| `app_integrations` | Integration |
| `app_prompt_versions`, `app_evaluation_results` | Domain |
| `saga_instances`, `outbox_messages` | Shared |

## Trace analytics scalability path

1. **Current (default):** native range partitioning on `traces(start_time)` — sufficient to hundreds of millions of rows
2. **When earned (>100M rows):** enable the TimescaleDB extension and run `create_hypertable('traces', 'StartTime')` — no client code changes needed

## Disabling startup schema creation

```bash
# appsettings.json or env var
Database__MigrateOnStartup=false
```
