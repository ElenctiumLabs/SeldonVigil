# Security Policy

## Supported Versions

| Version | Supported |
| ------- | --------- |
| 1.0.x   | Yes       |

## Reporting a Vulnerability

Please do not report security vulnerabilities through public issues.

Report them privately using a GitHub security advisory:
<https://github.com/ElenctiumLabs/SeldonVigil/security/advisories/new>

Expect an acknowledgement within three working days. If the report is accepted,
we will agree a fix and a coordinated disclosure date with you, and credit you
in the release notes unless you prefer to stay anonymous.

## Scope

- Remote code execution and authentication bypass
- Authorisation bypass and privilege escalation
- Cross-tenant data leakage
- SQL/NoSQL injection, path traversal, SSRF
- Secrets or credentials exposed in the repository or in a published image

## Out of Scope

- Dependency CVEs already surfaced by Dependabot
- Issues that require an already-compromised host
- Theoretical weaknesses with no realistic impact

## Hardening Notes

- Development-mode authentication (`/dev/token` and the `dev` password backdoor)
  is only reachable in the Development environment and must never be deployed.
  Production builds fail closed without a configured authority.
- Secrets belong in environment variables or a key vault, never in
  `appsettings.json`. `appsettings.Local.json`, `frontend/.env` and
  `infrastructure/docker/.env` are gitignored for that reason.
- Containers run as a non-root user and the API/worker images are distroless.
