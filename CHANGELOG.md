# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Modular monolith architecture (Identity, Security, Cost Intelligence, Observability,
  Domain, Governance, Analytics, AI Reasoning, Integration, Messaging)
- Docker Compose development and deployment stack
- PostgreSQL persistence with pgvector for vector search
- Redpanda event broker with outbox and saga patterns
- Prometheus metrics with optional OTLP trace/meter export
- Web UI served by nginx through the compose stack
- Development token endpoint for local testing (Development environment only)

### Removed
- Kubernetes manifests and Helm chart
- Terraform infrastructure modules
- Grafana, Seq, Garnet, Jaeger and Qdrant service dependencies
- Load testing with k6 and BenchmarkDotNet performance benchmarks
- Mutation testing with Stryker

### Changed
- Packages renamed to SeldonVigil.Sdk, SeldonVigil.Telemetry and SeldonVigil.Cli
- MediatR and AutoMapper pinned to their last open-source releases
- dotnet-ef bumped to 10.0.10
