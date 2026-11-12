# Integration Module

Manages third-party connector configurations. Supports full CRUD plus test, activate, deactivate, and delete operations.

## Supported connectors

Slack · Microsoft Teams · PagerDuty · Jira · GitHub · Webhook · Email

Each integration tracks `status`, `errorCount`, and `lastUsedAt`. The *test* action verifies connectivity before activation.
