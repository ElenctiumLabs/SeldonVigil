# CostIntelligence Module

Tracks LLM spend per agent/team/project/tenant against configurable budgets.

## Capabilities

- Budget creation with scoping, periods, alert thresholds, and enforcement types
- Cost dashboard: total spend, utilisation %, breakdowns by provider / model / agent
- Daily trend data for chart rendering
- AI-powered optimisation recommendations (`GET /api/v1/costs/recommendations`)
- Alert threshold notifications (e.g. 50%, 75%, 90% of budget)

## Budget enforcement types

- **Soft** — alert only; the agent continues operating
- **Hard** — block agent operations when the limit is reached
