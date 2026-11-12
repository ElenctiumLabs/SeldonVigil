# SeldonVigil.Sdk

Published NuGet package used by external .NET applications to integrate with SeldonVigil without hand-rolling HTTP calls.

## Clients

| Client | Methods |
|---|---|
| `SeldonVigilAuthenticationClient` | Login via API key (`/api/v1/auth/api-key`), token refresh on 401 |
| `AgentClient` | `GetAgentsAsync`, `RegisterAgentAsync`, `ActivateAgentAsync`, `SuspendAgentAsync`, `RevokeAgentAsync`, `GetAgentByIdAsync` |
| `ObservabilityClient` | `IngestTraceAsync`, `QueryTracesAsync`, `GetTraceAsync` |
| `SecurityClient` | `CreatePolicyAsync`, `GetPoliciesAsync`, `EvaluatePolicyAsync` → `{ isAllowed, violations[] }` |
| `CostIntelligenceClient` | Budgets, dashboard, recommendations |
| `GovernanceClient` | Governance policies and certification pipelines |
| `AIReasoningClient` | Root-cause analysis |
| `AnalyticsClient` | Notifications |
| `IntegrationClient` | Connector configurations |
| `SeldonVigilTraceExporter` | OpenTelemetry export bridge; `Export` is synchronous and returns `ExportResult.Failure` on error (no silent drops) |

## DI registration patterns

```csharp
// Generic
services.AddSeldonVigilSdk(authOptions, configOptions);

// API key flow
services.AddSeldonVigilSdkWithApiKey(baseUrl, apiKey);

// OAuth2 flow
services.AddSeldonVigilSdkWithOAuth2(baseUrl, clientId, secret);

// OTel auto-instrumentation
services.AddSeldonVigilSdkWithTraceExport(baseUrl, apiKey);
```
