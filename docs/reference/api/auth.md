# Auth Endpoints

All routes are prefixed `/api/v1/`. Auth endpoints do not require a Bearer token.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/auth/login` | Email + password → access token + refresh token |
| POST | `/api/v1/auth/api-key` | API key → access token + refresh token |
| POST | `/api/v1/auth/refresh` | Rotate refresh token |
| POST | `/api/v1/auth/logout` | Revoke refresh token |
| POST | `/api/v1/auth/register` | User registration (disabled by default) |
| GET | `/dev/token?role=Administrator` | Dev-only JWT — gated by `DevAuth:Enabled` |
