# CI/CD (GitHub Actions)

| Workflow | Trigger | Steps |
|---|---|---|
| `ci.yml` | Push / PR | Build → unit tests → API tests → dependency audit → Docker image builds |
| `release.yml` | Manual / tag | Release management, changelog, NuGet publish |
| `security-scan.yml` | Scheduled | Recurring security scan |
