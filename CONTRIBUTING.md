# Contributing to SeldonVigil

Thanks for your interest in contributing. This document explains how to set up a
development environment, run the tests, and get a pull request merged.

## Getting Started

```bash
git clone https://github.com/ElenctiumLabs/SeldonVigil.git
cd SeldonVigil
dotnet restore
dotnet build
dotnet test
```

The frontend lives in `frontend/` and the documentation viewer in
`tools/docs-viewer/`:

```bash
cd frontend
npm ci
npm run dev
```

## Prerequisites

- .NET 10 SDK
- Node.js 22 or newer
- PostgreSQL 18 or newer for the API integration tests
- Docker for the broker and the container builds

## Running the Tests

```bash
dotnet test tests/SeldonVigil.UnitTests         # fast, no external services
dotnet test tests/SeldonVigil.ApiTests          # boots the API host, needs PostgreSQL
dotnet test tests/SeldonVigil.IntegrationTests  # needs Docker
```

The full stack, including Redpanda and Prometheus, is described in
[docs/HOSTING_REQUIREMENTS.md](docs/HOSTING_REQUIREMENTS.md).

## Pull Requests

- Target `main`. Branch protection requires the `build` and `build-frontend`
  checks to pass and the branch to be up to date with `main`.
- Keep changes focused: one concern per pull request.
- Do not commit secrets, credentials or local configuration.
  `appsettings.Local.json`, `frontend/.env` and `infrastructure/docker/.env`
  are gitignored for exactly that reason.
- Update the documentation under `docs/` when behaviour changes. The docs viewer
  inlines that folder, so the published documentation always matches the repo.

## Style

- Run `dotnet format` before pushing.
- The build is configured with a strict analyzer set; aim for zero warnings.
- Match the conventions already used in the file you are editing.

## Commits

Write clear, imperative commit messages and reference the issue you are fixing.

## Reporting Security Issues

See [SECURITY.md](SECURITY.md). Do not open a public issue for vulnerabilities.
