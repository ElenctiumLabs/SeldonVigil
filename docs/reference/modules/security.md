# Security Module

Policy-based threat detection and automated response.

## Policy categories

RateLimiting · DataProtection · CostControl · Security · ResourceControl

## Evaluation modes

| Mode | Endpoint | When used |
|---|---|---|
| Synchronous input scan | `POST /api/v1/security/evaluate-input` | Pre-flight check before the agent acts; blocks or allows in real time |
| Async trace evaluation | `POST /api/v1/security/evaluate` | Post-hoc analysis; triggered by a worker or on demand |

## Auto-responses

Block · Throttle · Alert

Each policy can define a notify list for alerts. Security violations are raised with `status`: Open / Resolved / Suppressed.

## Seed data

Default security policies are created on first run.
