# SeldonVigil SDK

Official .NET SDK for SeldonVigil.

## Features

- **Authentication**: JWT Bearer token authentication with automatic token refresh
- **Agent Management**: Register, activate, suspend, and manage AI agents
- **Security Policies**: Create, evaluate, and manage security policies
- **Observability**: Ingest traces and spans for distributed tracing
- **OpenTelemetry Integration**: Automatic trace export to the SeldonVigil platform
- **Dependency Injection**: Easy integration with Microsoft.Extensions.DependencyInjection

## Installation

```bash
dotnet add package SeldonVigil.Sdk
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Sdk.Extensions;

// Configure the SDK
var services = new ServiceCollection();

services.AddSeldonVigilSdk(
    authOptions =>
    {
        authOptions.ApiKey = "your-api-key";
        authOptions.BaseUrl = "http://localhost:8080";
    },
    configOptions =>
    {
        configOptions.BaseUrl = "http://localhost:8080";
        configOptions.ApiVersion = "v1";
    });

var serviceProvider = services.BuildServiceProvider();

// Use the SDK
var agentClient = serviceProvider.GetRequiredService<AgentClient>();
var agents = await agentClient.GetAgentsAsync();
```

## Configuration

### API Key Authentication

```csharp
services.AddSeldonVigilSdk(authOptions =>
{
    authOptions.ApiKey = Environment.GetEnvironmentVariable("SELDONVIGIL_API_KEY");
    authOptions.BaseUrl = "http://localhost:8080";
});
```

### OAuth2 Authentication

```csharp
services.AddSeldonVigilSdk(authOptions =>
{
    authOptions.ClientId = "your-client-id";
    authOptions.ClientSecret = "your-client-secret";
    authOptions.Authority = "https://your-auth-server.com";
    authOptions.Scope = "api.read api.write";
});
```

### OpenTelemetry Integration

```csharp
services.AddSeldonVigilSdk(
    authOptions => { /* ... */ },
    configOptions => { /* ... */ },
    telemetryOptions =>
    {
        telemetryOptions.EnableTraceExport = true;
        telemetryOptions.ServiceName = "my-service";
    });
```

## Usage Examples

### Agent Management

```csharp
var agentClient = serviceProvider.GetRequiredService<AgentClient>();

// List all agents
var agents = await agentClient.GetAgentsAsync();

// Register a new agent
var newAgent = await agentClient.RegisterAgentAsync(new CreateAgentRequest
{
    ExternalId = "agent-001",
    Name = "My AI Agent",
    Framework = "LangChain",
    ModelProvider = "OpenAI",
    ModelName = "gpt-4"
});

// Activate an agent
var activatedAgent = await agentClient.ActivateAgentAsync(agentId, "Production deployment");
```

### Security Policy Evaluation

```csharp
var securityClient = serviceProvider.GetRequiredService<SecurityClient>();

// Evaluate a request against policies
var result = await securityClient.EvaluatePolicyAsync(new PolicyEvaluation
{
    AgentId = "agent-001",
    Action = "data_access",
    Resource = "customer_database"
});

if (result.IsAllowed)
{
    // Proceed with the action
}
```

### Observability

```csharp
var observabilityClient = serviceProvider.GetRequiredService<ObservabilityClient>();

// Ingest a trace
var traceId = await observabilityClient.IngestTraceAsync(new SeldonVigilTrace
{
    AgentId = "agent-001",
    OperationName = "ProcessRequest",
    Status = TraceStatus.Ok,
    DurationMs = 150
});
```

## License

MIT License - see LICENSE file for details.

## Support

For issues and questions, please visit our GitHub repository.
