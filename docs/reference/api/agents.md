# Agent Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/agents` | Register a new agent |
| GET | `/api/v1/agents` | List agents — filter: status, framework, modelProvider, page, pageSize |
| GET | `/api/v1/agents/{id}` | Get agent by ID |
| POST | `/api/v1/agents/{id}/activate` | Activate a Pending agent |
| POST | `/api/v1/agents/{id}/suspend` | Suspend an Active agent |
| POST | `/api/v1/agents/{id}/revoke` | Permanently revoke an agent |
