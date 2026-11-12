# Governance Module

Manages compliance documentation and agent certification.

## Governance Policies

YAML-defined, versioned policy documents. Lifecycle: **Draft → Active → Deprecated**. Separate from Security Policies — used for audit trails and compliance reporting rather than runtime enforcement.

## Certification Pipelines

Multi-stage approval workflow for promoting an agent to production. Each pipeline is triggered for a specific agent. Stages are reviewed and approved/rejected. A full audit trail is maintained.
