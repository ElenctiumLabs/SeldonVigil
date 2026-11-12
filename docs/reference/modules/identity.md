# Identity Module

Handles authentication, authorisation, agent registration, and tenant management.

## Capabilities

- User registration (opt-in, disabled by default via `Authentication:AllowRegistration`)
- JWT login / refresh / logout with refresh-token rotation
- API-key login (→ JWT)
- Role-based access control (Administrator vs standard roles)
- Agent CRUD + full lifecycle management (Pending → Active → Suspended → Revoked)
- Agent trust relationships
- Tenant management

## Seed data

On first run a default tenant (ID: `00000000-0000-0000-0000-000000000001`) and an admin user are created. See `docs/HOSTING_REQUIREMENTS.md` for credentials.
