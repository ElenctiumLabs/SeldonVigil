# Pages

| Route | Page | Key features |
|---|---|---|
| `/` | Dashboard | Live stat cards (agents, traces, security events, cost); recent activity feed; system health |
| `/agents` | Agents | Agent list with status; activate an agent. Registration and suspension are CLI-only |
| `/traces` | Traces | Query and list traces by agent/status/date range; trace detail view |
| `/security` | Security | Violations list with filtering; security dashboard. Security policies are managed through the CLI |
| `/costs` | Costs | Total spend vs budget; daily trend chart; breakdowns by provider/model/agent; create budget. Budget reset is CLI-only |
| `/broker` | Broker | Broker status; topic list; consumer groups + lag; topic message browser |
| `/governance` | Governance | Policy lifecycle (YAML); create/activate/view policies |
| `/governance/certification` | Certification | Certification pipelines; trigger new certification; approve stages |
| `/integrations` | Integrations | Connector list; create/test/activate/deactivate/delete |
| `/ai-reasoning` | AI Reasoning | Trigger RCA; view RCA history; confidence, evidence, actions |
| `/reference` | Reference | This documentation, generated from `docs/` |
| `/settings` | Settings | Layout picker (stored in the browser); theme, notifications, budget and API settings shown read-only, with editing marked coming soon |
| `/login` | Login | Email + password auth |

## Management surfaces

The dashboard is an operations surface. It shows state and exposes a few workflow actions:
activate an agent, create and activate a governance policy, trigger and approve a
certification, manage integrations, and trigger a root-cause analysis.

Everything else is managed through the CLI, which calls the same API:

| Entity | Commands |
|---|---|
| Agents | `seldonvigil agent create`, `suspend`, `activate`, `delete`, `list`, `get` |
| Security policies | `seldonvigil policy create`, `disable`, `evaluate`, `list`, `get` |
| Budgets | `seldonvigil budget create`, `list`, `reset` |
| Traces | `seldonvigil trace list`, `get`, `export` |
| Demo data | `seldonvigil seed` |

Controls that are displayed but not yet wired up carry a **Coming soon** marker, so an
inert input is never mistaken for a working one.
