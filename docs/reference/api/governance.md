# Governance Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/governance/policies` | Create governance policy |
| GET | `/api/v1/governance/policies` | List governance policies |
| POST | `/api/v1/governance/policies/{id}/activate` | Activate a Draft policy |
| POST | `/api/v1/governance/certification/pipelines` | Trigger a certification pipeline for an agent |
| GET | `/api/v1/governance/certification/pipelines` | List all pipelines |
| POST | `/api/v1/governance/certification/pipelines/{id}/approve` | Approve a pipeline stage |
