# Security Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/security/policies` | Create policy (admin-only) |
| GET | `/api/v1/security/policies` | List — filter: category, status, page, pageSize |
| GET | `/api/v1/security/policies/{id}` | Get policy by ID |
| PUT | `/api/v1/security/policies/{id}` | Update policy |
| DELETE | `/api/v1/security/policies/{id}` | Disable policy |
| POST | `/api/v1/security/evaluate` | Post-hoc trace evaluation |
| POST | `/api/v1/security/evaluate-input` | Synchronous input scan → `{ isAllowed, violations[] }` |
| GET | `/api/v1/security/violations` | List violations — filter: agentId, severity, status, from, to |
| GET | `/api/v1/security/dashboard` | Security dashboard metrics |
