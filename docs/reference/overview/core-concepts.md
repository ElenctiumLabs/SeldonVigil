# Core Concepts

## Agent

An AI-powered software component registered with SeldonVigil. Has a `framework`, `modelProvider`, `modelName`, and optional `CapabilityManifest`. Lifecycle states:

```mermaid
flowchart LR
    Pending --> Active --> Suspended --> Revoked
```

Agents can have trust relationships with other agents.

## Trace

A record of a single agent operation. Key fields: `operationName`, `operationType` (LlmCall, Custom…), `status` (Ok, Error, Timeout), `startTime`/`endTime`, `tokenUsage` (input/output/cached), `cost`, `attributes`, `events`, `links`.

Ingestion publishes a `TraceIngestedEvent` to the event broker and synchronously records cost.

## Security Policy

Rule-based policy with `category` (RateLimiting, DataProtection, CostControl, Security, ResourceControl), `severity` (Low → Critical), and a `RuleDefinition` evaluated against trace attributes.

Has an `AutoResponse`: Block / Throttle / Alert + notify list. Evaluated synchronously (input scan) or asynchronously (post-ingestion).

## Cost Budget

Scoped to Tenant / Team / Project / Agent. Fields: `budgetAmount`, `currency`, `period` (Daily → Annual), `alertThresholds` (e.g. `[0.5, 0.75, 0.9]`), `enforcementType` (Soft / Hard).

Spend is recorded inline on every trace ingestion.

## Governance Policy

YAML-defined, versioned policy used for compliance and audit documentation. Separate from Security Policy. Lifecycle: **Draft → Active → Deprecated**.

## Certification Pipeline

Multi-stage approval workflow for certifying an agent for production. Each stage has a status and output. Triggered explicitly; stages are reviewed and approved.

## Root Cause Analysis

AI-powered analysis tying an anomaly to a probable root cause. Returns `confidence` (0–1), `evidence[]`, `contributingFactors[]`, `recommendedActions[]`.

## Tenant

Top-level isolation unit. A default tenant is seeded with ID `00000000-0000-0000-0000-000000000001`. Multi-tenancy is enforced via EF global query filters sourced from JWT claims.

## Integration

Configuration for a third-party connector (Slack, Teams, PagerDuty, Jira, GitHub, SMTP). Has `status`, `errorCount`, `lastUsedAt`. Supports test, activate, deactivate, delete.
