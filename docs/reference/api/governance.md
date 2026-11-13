# Governance Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/governance/policies` | Create governance policy |
| GET | `/api/v1/governance/policies` | List governance policies for the tenant |
| GET | `/api/v1/governance/policies/{id}` | Get a governance policy by ID |
| POST | `/api/v1/governance/policies/{id}/activate` | Activate a Draft policy |
| POST | `/api/v1/governance/certification/trigger` | Trigger a certification pipeline for an agent |
| GET | `/api/v1/governance/certification` | List the tenant's certification pipelines |
| GET | `/api/v1/governance/certification/agent/{agentId}` | List the pipelines for one agent |
| GET | `/api/v1/governance/certification/{id}` | Get a certification pipeline by ID |
| POST | `/api/v1/governance/certification/{id}/approve` | Approve a pipeline stage |
