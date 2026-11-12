# SeldonVigil Platform - Comprehensive User Guide

## Table of Contents

1. [System Overview](#system-overview)
2. [Prerequisites](#prerequisites)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Quick Start](#quick-start)
6. [Use Case Scenarios](#use-case-scenarios)
7. [API Usage](#api-usage)
8. [SDK Usage](#sdk-usage)
9. [CLI Usage](#cli-usage)
10. [Monitoring & Observability](#monitoring--observability)
11. [Deployment](#deployment)
12. [Troubleshooting](#troubleshooting)
13. [Security Best Practices](#security-best-practices)

---

## System Overview

**SeldonVigil** is an enterprise-grade observability, security, and governance platform for AI agent systems. It provides comprehensive monitoring, policy enforcement, cost tracking, and intelligent analysis capabilities for AI-powered applications.

### Architecture

SeldonVigil follows a modular monolith architecture with clear domain boundaries:

- **SeldonVigil.Api** - ASP.NET Core REST API with modular endpoints
- **SeldonVigil.Workers** - Background processing services for async operations
- **SeldonVigil.Sdk** - .NET client SDK for application integration
- **SeldonVigil.Cli** - Command-line interface for administrative tasks
- **SeldonVigil.SharedKernel** - Shared domain logic and infrastructure
- **SeldonVigil.SourceGenerators** - Roslyn source generators for compile-time optimizations

### Core Modules

| Module | Description | Key Features |
|--------|-------------|--------------|
| **Identity** | Authentication & authorization | JWT-based auth, role-based access control, agent management |
| **Observability** | Tracing, metrics, logging | OpenTelemetry integration, distributed tracing, structured logging |
| **CostIntelligence** | Cloud cost optimization | Budget tracking, spend analysis, alert thresholds, cost optimization recommendations |
| **Security** | Threat detection & posture | Policy evaluation, threat detection, automated responses, compliance monitoring |
| **Governance** | Policy enforcement & compliance | Policy lifecycle management, audit trails, compliance reporting |
| **AIReasoning** | Intelligent analysis | Root cause analysis, anomaly detection, predictive insights |
| **Analytics** | Data processing & insights | Custom dashboards, trend analysis, reporting |
| **Integration** | Third-party connectors | Extensible integration framework for external systems |

---

## Prerequisites

### Software Requirements

#### Development Environment
- **.NET SDK 10.0** or later
- **Docker Desktop** 4.0+ (for local infrastructure)
- **Docker Compose** 2.0+
- **Git** for version control
- **PowerShell** 7+ (Windows) or **Bash** (Linux/macOS)

#### Production Environment
- **Docker** 24+ and **Docker Compose** 2.0+
- **PostgreSQL** 15+ with the `vector` extension
- **Redpanda** 26.x+ (the compose stack runs a single-node broker)

### External Services

#### Required Services
1. **PostgreSQL Database** - Primary data store
   - Version: 16 or later
   - Minimum storage: 100GB (scalable)
   - Recommended: Managed service (AWS RDS, Azure Database, etc.)

2. **Redis Cache** - Caching and session storage
   - Version: 7 or later
   - Minimum memory: 512MB
   - Recommended: Managed service (ElastiCache, Azure Cache, etc.)

3. **Redpanda** - Event streaming
   - Version: 26.x or later
   - Minimum partitions: 3
   - Recommended: Managed service (Redpanda Cloud, or self-managed with TLS/SASL)

#### Optional Services (for full functionality)
4. **Prometheus** - Metrics collection
   - Version: 2.53 or later
   - Alternative: Prometheus Cloud, CloudWatch

5. **OpenAI API** - AI capabilities
   - API Key required
   - Models: GPT-4, GPT-3.5-turbo
   - Required for: AIReasoning module

6. **Azure Key Vault** - Secret management (optional)
    - Set `AZURE_KEYVAULT_NAME` to load secrets from Key Vault; when unset, secrets come from environment variables
    - Alternative: HashiCorp Vault, AWS Secrets Manager

### Hardware Requirements

#### Minimum (Development)
- CPU: 4 cores
- RAM: 8GB
- Storage: 50GB SSD

#### Recommended (Production)
- CPU: 8+ cores
- RAM: 16GB+
- Storage: 200GB+ SSD
- Network: 1Gbps

#### High Availability (Production)
- Multiple API replicas behind a load balancer
- Multiple worker replicas (`docker compose up -d --scale worker=4`)
- Database replication
- Redpanda cluster with multiple brokers

---

## Installation

### Local Development Setup

#### 1. Clone the Repository

```bash
git clone <repository-url>
cd SeldonVigil
```

#### 2. Install .NET SDK

Download and install .NET SDK 10.0 from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

Verify installation:
```bash
dotnet --version
```

#### 3. Install Docker Desktop

Download Docker Desktop from [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)

Verify installation:
```bash
docker --version
docker-compose --version
```

#### 4. Set Up Environment Variables

Create a `.env` file in the project root:

```bash
# Database
POSTGRES_PASSWORD=your_secure_password_here

# Authentication
AUTHORITY=https://auth.acme.example
AUDIENCE=seldonvigil-api

# OpenAI
OPENAI_API_KEY=your_openai_api_key_here
```

#### 5. Start Infrastructure Services

PostgreSQL is **not** containerised - install and run it on your
machine first (`localhost:5432`). Redpanda is containerised via Docker Compose.
The containerised services reach PostgreSQL via `host.docker.internal`.

```bash
cd infrastructure/docker
docker-compose up -d
```

This starts:
- Redpanda (port 9092)
- Prometheus (port 9090)

#### 6. Run Database Migrations

```bash
cd src/SeldonVigil.Api
dotnet ef database update --project Modules/Identity/Infrastructure/Persistence
```

#### 7. Build and Run the API

```bash
dotnet restore
dotnet build
dotnet run --project src/SeldonVigil.Api
```

The API will be available at `http://localhost:8080`

#### 8. Run Background Workers

In a separate terminal:

```bash
dotnet run --project src/SeldonVigil.Workers
```

### Production Deployment

> **WARNING — the default compose stack is a development configuration.**
> `infrastructure/docker/docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT=Development`
> and `DOTNET_ENVIRONMENT=Development`, which enables the `/dev/token` endpoint and
> the development login backdoor (any username with password `dev` returns an
> administrator token). Do not expose it to an untrusted network. Use the
> production overrides for anything real.

The production path is the same stack with the production overrides applied:

```bash
cd infrastructure/docker
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --scale worker=4
```

The overrides set `Production`, which disables dev auth and turns on the
production configuration checks. You must supply an OIDC authority and audience,
the broker address, and broker TLS/SASL credentials; the API refuses to start
without them. See [HOSTING_REQUIREMENTS.md](HOSTING_REQUIREMENTS.md) for the full
production configuration, sizing and hardening.

---

## Configuration

### Application Configuration

Configuration is managed through `appsettings.json` and environment variables.

#### Development Configuration (`appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=seldonvigil;Username=seldonvigil;Password=your_password",
    "Redis": "localhost:6379",
    "Broker": "localhost:9092"
  },
  "Authentication": {
    "Authority": "https://auth.acme.example",
    "Audience": "seldonvigil-api",
    "RequireHttpsMetadata": false
  },
  "OpenAI": {
    "ApiKey": "your_openai_api_key",
    "Model": "gpt-4"
  },
  "Otlp": {
    "Endpoint": "http://localhost:4317"
  },
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "Console"
      }
    ]
  }
}
```

#### Production Configuration

Use environment variables for sensitive data:

```bash
export ConnectionStrings__PostgreSQL="Host=prod-db;Database=seldonvigil;Username=seldonvigil;Password=${POSTGRES_PASSWORD}"
export ConnectionStrings__Redis="prod-redis:6379"
export ConnectionStrings__Broker="prod-redpanda:9092"
export Authentication__Authority="https://auth.acme.example"
export Authentication__Audience="seldonvigil-api"
export OpenAI__ApiKey="${OPENAI_API_KEY}"
# Optional: export Otlp__Endpoint="http://collector:4317"
```

### Module-Specific Configuration

#### Cost Intelligence Module

```json
{
  "CostIntelligence": {
    "DefaultCurrency": "USD",
    "AlertThresholds": [0.5, 0.75, 0.9],
    "BudgetPeriod": "Monthly",
    "PricingModel": "PerToken"
  }
}
```

#### Security Module

```json
{
  "Security": {
    "AutoResponseEnabled": true,
    "DefaultSeverity": "Medium",
    "PolicyEvaluationInterval": "00:05:00"
  }
}
```

#### Observability Module

```json
{
  "Observability": {
    "TraceSamplingRate": 0.1,
    "MetricsEnabled": true,
    "LogRetentionDays": 30
  }
}
```

---

## Quick Start

### 1. Verify Infrastructure is Running

```bash
docker-compose ps
```

All containerised services should show as "Up". Also confirm your local
PostgreSQL (`pg_isready -h localhost -p 5432`) is running, since it is not
managed by Docker Compose. Redpanda runs in Docker (`localhost:9092`).

### 2. Access the API

Open your browser to `http://localhost:8080/swagger` to view the Swagger UI.

### 3. Generate a Development Token

```bash
curl -X GET "http://localhost:8080/dev/token?role=Administrator" -H "accept: application/json"
```

Copy the returned `token` value.

### 4. Create Your First Agent

```bash
curl -X POST "http://localhost:8080/api/agents" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My First Agent",
    "description": "A test agent",
    "type": "Chat",
    "capabilities": ["text-generation", "code-generation"]
  }'
```

### 5. Ingest a Trace

```bash
curl -X POST "http://localhost:8080/api/observability/traces" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "YOUR_AGENT_ID",
    "operation": "generate_text",
    "status": "Completed",
    "duration": 1500,
    "tokenUsage": {
      "promptTokens": 100,
      "completionTokens": 200,
      "totalTokens": 300
    },
    "cost": {
      "amount": 0.006,
      "currency": "USD"
    }
  }'
```

### 6. View Metrics

1. Navigate to Prometheus at `http://localhost:9090`
2. Run queries against the `SeldonVigil.Api` metrics

---

## Use Case Scenarios

### Scenario 1: Monitoring AI Agent Performance

**Objective:** Track the performance, latency, and error rates of AI agents in production.

**Steps:**

1. **Enable Tracing**
   - Configure OpenTelemetry in your application
   - Set sampling rate appropriately (e.g., 10% for high-traffic systems)

2. **Ingest Traces**
   - Use the SeldonVigil SDK to automatically send traces
   - Include custom attributes for business context

3. **Monitor Traces** (optional)
   - Set `OpenTelemetry:OtlpEndpoint` to an OTLP collector
   - Search traces by agent ID, operation, or time range

4. **Set Up Alerts**
   - Configure Prometheus alerts for high latency
   - Set thresholds based on SLA requirements

**Expected Outcome:** Real-time visibility into agent performance with automated alerting.

### Scenario 2: Cost Management and Budgeting

**Objective:** Control and optimize AI/ML costs across all agents.

**Steps:**

1. **Create Cost Budgets**
   ```bash
   curl -X POST "http://localhost:8080/api/cost/budgets" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Monthly Production Budget",
       "budgetAmount": {"amount": 1000, "currency": "USD"},
       "period": "Monthly",
       "alertThresholds": [0.5, 0.75, 0.9]
     }'
   ```

2. **Assign Budgets to Agents**
   - Link budgets to specific agents or tenant
   - Set spending limits per agent

3. **Monitor Spend**
   - View cost metrics in Prometheus
   - Track spend against budget thresholds
   - Receive alerts when thresholds are breached

4. **Optimize Costs**
   - Review cost optimization recommendations
   - Implement caching strategies
   - Adjust model selection based on cost/benefit analysis

**Expected Outcome:** Predictable costs with automated alerts and optimization recommendations.

### Scenario 3: Security Policy Enforcement

**Objective:** Automatically detect and respond to security threats in AI agent behavior.

**Steps:**

1. **Define Security Policies**
   ```bash
   curl -X POST "http://localhost:8080/api/security/policies" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "PII Detection Policy",
       "description": "Detect PII in agent outputs",
       "category": "DataProtection",
       "rules": [
         {
           "metric": "output_text",
           "operator": "contains",
           "value": "ssn_pattern",
           "threshold": 1
         }
       ],
       "severity": "High",
       "autoResponse": {
         "action": "Block",
         "notify": ["security-team"]
       }
     }'
   ```

2. **Activate Policies**
   - Enable policies for specific agents or tenants
   - Set evaluation frequency

3. **Monitor Policy Violations**
   - View security metrics in Prometheus
   - Review violation logs and audit trails

4. **Respond to Incidents**
   - Automated responses trigger based on policy configuration
   - Manual investigation using audit logs
   - Policy refinement based on false positives

**Expected Outcome:** Automated threat detection with immediate response capabilities.

### Scenario 4: Root Cause Analysis

**Objective:** Automatically analyze incidents to determine root causes using AI.

**Steps:**

1. **Enable AIReasoning Module**
   - Configure OpenAI API key
   - Ensure the PostgreSQL `vector` extension is installed (pgvector)

2. **Trigger Analysis**
   ```bash
   curl -X POST "http://localhost:8080/api/ai-reasoning/root-cause" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "incidentId": "INCIDENT_ID",
       "timeRange": {
         "from": "2024-01-01T00:00:00Z",
         "to": "2024-01-02T00:00:00Z"
       }
     }'
   ```

3. **Review Analysis Results**
   - View identified root causes
   - Review recommended actions
   - Access supporting evidence

4. **Implement Recommendations**
   - Apply suggested fixes
   - Monitor for recurrence

**Expected Outcome:** Faster incident resolution with AI-powered insights.

### Scenario 5: Multi-Tenant Governance

**Objective:** Manage multiple tenants with different governance requirements.

**Steps:**

1. **Create Tenants**
   ```bash
   curl -X POST "http://localhost:8080/api/tenants" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Acme Corp",
       "domain": "acme.com",
       "settings": {
         "complianceLevel": "SOC2",
         "dataResidency": "US"
       }
     }'
   ```

2. **Configure Tenant-Specific Policies**
   - Apply security policies per tenant
   - Set cost budgets per tenant
   - Configure retention policies

3. **Monitor Tenant Activity**
   - View tenant-specific dashboards
   - Generate compliance reports
   - Audit tenant access

4. **Manage Tenant Lifecycle**
   - Onboard new tenants
   - Offboard terminated tenants
   - Migrate tenant data

**Expected Outcome:** Isolated multi-tenant environment with per-tenant governance.

---

## API Usage

### Authentication

All API endpoints require authentication via JWT tokens.

#### Get Development Token (Development Only)

```bash
GET /dev/token?role=Administrator&hours=24
```

#### Authenticate with Provider (Production)

```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "user@acme.example",
  "password": "password"
}
```

Returns:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-02T00:00:00Z"
}
```

#### Use Token in Requests

```bash
Authorization: Bearer YOUR_TOKEN
```

### Agent Management

#### Create Agent

```bash
POST /api/agents
Content-Type: application/json

{
  "name": "Customer Support Bot",
  "description": "Handles customer inquiries",
  "type": "Chat",
  "capabilities": ["text-generation", "sentiment-analysis"],
  "configuration": {
    "model": "gpt-4",
    "temperature": 0.7,
    "maxTokens": 1000
  }
}
```

#### List Agents

```bash
GET /api/agents?page=1&pageSize=20
```

#### Get Agent Details

```bash
GET /api/agents/{agentId}
```

#### Update Agent

```bash
PUT /api/agents/{agentId}
Content-Type: application/json

{
  "name": "Updated Name",
  "status": "Active"
}
```

#### Suspend Agent

```bash
POST /api/agents/{agentId}/suspend
```

### Observability

#### Ingest Trace

```bash
POST /api/observability/traces
Content-Type: application/json

{
  "agentId": "agent-id",
  "operation": "generate_text",
  "status": "Completed",
  "duration": 1500,
  "input": {
    "prompt": "Hello, world!"
  },
  "output": {
    "text": "Hi there!"
  },
  "tokenUsage": {
    "promptTokens": 100,
    "completionTokens": 200,
    "totalTokens": 300
  },
  "cost": {
    "amount": 0.006,
    "currency": "USD"
  },
  "attributes": {
    "userId": "user-123",
    "sessionId": "session-456"
  }
}
```

#### Query Traces

```bash
GET /api/observability/traces?agentId={agentId}&from=2024-01-01&to=2024-01-02
```

#### Get Trace by ID

```bash
GET /api/observability/traces/{traceId}
```

### Cost Intelligence

#### Create Budget

```bash
POST /api/cost/budgets
Content-Type: application/json

{
  "name": "Monthly Budget",
  "budgetAmount": {
    "amount": 1000,
    "currency": "USD"
  },
  "period": "Monthly",
  "alertThresholds": [0.5, 0.75, 0.9],
  "agentId": "agent-id"
}
```

#### Record Spend

```bash
POST /api/cost/spend
Content-Type: application/json

{
  "agentId": "agent-id",
  "amount": {
    "amount": 0.01,
    "currency": "USD"
  },
  "operation": "text_generation"
}
```

#### Get Budget Status

```bash
GET /api/cost/budgets/{budgetId}/status
```

#### Get Cost Recommendations

```bash
GET /api/cost/recommendations?agentId={agentId}
```

### Security

#### Create Policy

```bash
POST /api/security/policies
Content-Type: application/json

{
  "name": "Rate Limiting Policy",
  "description": "Limit requests per minute",
  "category": "RateLimiting",
  "rules": [
    {
      "metric": "request_count",
      "operator": "greater_than",
      "value": 100,
      "timeWindow": "00:01:00"
    }
  ],
  "severity": "Medium",
  "autoResponse": {
    "action": "Throttle",
    "parameters": {
      "limit": 50
    }
  }
}
```

#### Evaluate Policy

```bash
POST /api/security/policies/{policyId}/evaluate
Content-Type: application/json

{
  "traceId": "trace-id"
}
```

#### List Policies

```bash
GET /api/security/policies?category=Security
```

### Governance

#### Create Audit Log Entry

```bash
POST /api/governance/audit
Content-Type: application/json

{
  "action": "AgentCreated",
  "actor": "user-123",
  "resource": "agent-456",
  "details": {
    "name": "New Agent"
  }
}
```

#### Query Audit Logs

```bash
GET /api/governance/audit?actor={actorId}&from=2024-01-01&to=2024-01-02
```

#### Generate Compliance Report

```bash
POST /api/governance/compliance/reports
Content-Type: application/json

{
  "standard": "SOC2",
  "period": {
    "from": "2024-01-01",
    "to": "2024-01-31"
  }
}
```

---

## SDK Usage

### Installation

```bash
dotnet add package SeldonVigil.Sdk
```

### Configuration

```csharp
using SeldonVigil.Sdk;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add SeldonVigil SDK with API Key
services.AddSeldonVigilSdkWithApiKey(options =>
{
    options.BaseUrl = "https://api.acme.example";
    options.ApiKey = "your-api-key";
});

// Or with OAuth2
services.AddSeldonVigilSdkWithOAuth2(new Uri("https://auth.acme.example"), options =>
{
    options.BaseUrl = "https://api.acme.example";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
});

var serviceProvider = services.BuildServiceProvider();
```

### Observability Client

```csharp
using SeldonVigil.Sdk.Observability;

var observabilityClient = serviceProvider.GetRequiredService<ObservabilityClient>();

// Ingest a trace
var trace = new SeldonVigilTrace
{
    Id = Guid.NewGuid(),
    TenantId = "tenant-123",
    AgentId = "agent-456",
    Operation = "generate_text",
    Status = TraceStatus.Completed,
    Duration = TimeSpan.FromMilliseconds(1500),
    TokenUsage = new TokenUsage
    {
        PromptTokens = 100,
        CompletionTokens = 200,
        TotalTokens = 300
    },
    Cost = new Cost
    {
        Amount = 0.006m,
        Currency = "USD"
    }
};

await observabilityClient.IngestTraceAsync(trace);

// Query traces
var traces = await observabilityClient.QueryTracesAsync(
    agentId: "agent-456",
    from: DateTime.UtcNow.AddDays(-1),
    to: DateTime.UtcNow
);

// Get trace by ID
var trace = await observabilityClient.GetTraceAsync(traceId);
```

### Security Client

```csharp
using SeldonVigil.Sdk.Security;

var securityClient = serviceProvider.GetRequiredService<SecurityClient>();

// Create a policy
var policy = await securityClient.CreatePolicyAsync(new CreatePolicyRequest
{
    Name = "PII Detection",
    Description = "Detect PII in outputs",
    Category = PolicyCategory.DataProtection,
    Rules = new List<PolicyRule>
    {
        new PolicyRule
        {
            Metric = "output_text",
            Operator = "contains",
            Value = "ssn_pattern",
            Threshold = 1
        }
    },
    Severity = Severity.High,
    AutoResponse = new AutoResponse
    {
        Action = "Block",
        Notify = new[] { "security-team" }
    }
});

// Evaluate a trace against policies
var result = await securityClient.EvaluatePolicyAsync(policyId, trace);

// List policies
var policies = await securityClient.GetPoliciesAsync();
```

### Agent Client

```csharp
using SeldonVigil.Sdk.Agents;

var agentClient = serviceProvider.GetRequiredService<AgentClient>();

// Create an agent
var agent = await agentClient.CreateAgentAsync(new CreateAgentRequest
{
    Name = "Customer Support Bot",
    Description = "Handles customer inquiries",
    Type = AgentType.Chat,
    Capabilities = new[] { "text-generation", "sentiment-analysis" }
});

// List agents
var agents = await agentClient.GetAgentsAsync();

// Suspend an agent
await agentClient.SuspendAgentAsync(agentId);

// Update agent manifest
await agentClient.UpdateAgentManifestAsync(agentId, new AgentManifest
{
    Model = "gpt-4",
    Temperature = 0.7,
    MaxTokens = 1000
});
```

### OpenTelemetry Integration

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;
using SeldonVigil.Sdk.OpenTelemetry;

var builder = services.AddOpenTelemetry();

builder
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSource("MyApplication")
    .AddSeldonVigilTraceExporter(options =>
    {
        options.Endpoint = "https://api.acme.example";
        options.ApiKey = "your-api-key";
    });

var tracerProvider = builder.Build();
```

---

## CLI Usage

### Installation

The CLI is distributed as a .NET tool:

```bash
dotnet tool install --global SeldonVigil.Cli --version 1.0.0
```

### Authentication

```bash
# Login with API key
seldonvigil login --api-key YOUR_API_KEY

# Login with OAuth2
seldonvigil login --oauth2 --client-id YOUR_CLIENT_ID

# Check current session
seldonvigil whoami

# Logout
seldonvigil logout
```

### Agent Commands

```bash
# List all agents
seldonvigil agent list

# Get agent details
seldonvigil agent get AGENT_ID

# Create a new agent
seldonvigil agent create \
  --name "My Agent" \
  --type Chat \
  --capabilities text-generation,sentiment-analysis

# Suspend an agent
seldonvigil agent suspend AGENT_ID

# Activate an agent
seldonvigil agent activate AGENT_ID

# Delete an agent
seldonvigil agent delete AGENT_ID
```

### Policy Commands

```bash
# List all policies
seldonvigil policy list

# Get policy details
seldonvigil policy get POLICY_ID

# Create a new policy
seldonvigil policy create \
  --name "Rate Limiting" \
  --category RateLimiting \
  --severity Medium \
  --file policy.json

# Activate a policy
seldonvigil policy activate POLICY_ID

# Deactivate a policy
seldonvigil policy deactivate POLICY_ID

# Delete a policy
seldonvigil policy delete POLICY_ID
```

### Budget Commands

```bash
# List all budgets
seldonvigil budget list

# Get budget status
seldonvigil budget status BUDGET_ID

# Create a new budget
seldonvigil budget create \
  --name "Monthly Budget" \
  --amount 1000 \
  --currency USD \
  --period Monthly \
  --thresholds 0.5,0.75,0.9

# Get cost recommendations
seldonvigil budget recommendations --agent AGENT_ID
```

### Observability Commands

```bash
# Query traces
seldonvigil observability traces \
  --agent AGENT_ID \
  --from 2024-01-01 \
  --to 2024-01-02

# Get trace details
seldonvigil observability trace TRACE_ID

# Get agent statistics
seldonvigil observability stats AGENT_ID
```

### Output Formatting

```bash
# JSON output
seldonvigil agent list --output json

# Table output (default)
seldonvigil agent list --output table

# YAML output
seldonvigil agent list --output yaml
```

---

## Monitoring & Observability

### Grafana (optional)

Grafana is not shipped in `docker-compose.yml`. An optional dashboard JSON lives at
`infrastructure/grafana/seldonvigil-dashboard.json` if you run Grafana alongside Prometheus.

### Prometheus Metrics

SeldonVigil exposes metrics at `/metrics` endpoint in Prometheus format.

#### Key Metrics

- `seldonvigil_agent_requests_total` - Total requests per agent
- `seldonvigil_agent_duration_seconds` - Request duration
- `seldonvigil_agent_errors_total` - Error count
- `seldonvigil_cost_total` - Total cost incurred
- `seldonvigil_policy_violations_total` - Policy violation count
- `seldonvigil_trace_ingested_total` - Traces ingested

### Tracing (optional)

Set `OpenTelemetry:OtlpEndpoint` to export traces and meters to any OTLP collector
(for example Jaeger or an OpenTelemetry Collector). No collector is shipped in the
default compose stack.

### Logs

Logs are written to the console by Serilog. There is no log server in the default
deployment; pipe the console output to your own log store if needed.

### Health Checks

Health check endpoint: `/health`

```bash
curl http://localhost:8080/health
```

Response:
```json
{
  "status": "Healthy",
  "checks": {
    "postgresql": "Healthy",
    "authentication": "Healthy",
    "broker": "Healthy"
  }
}
```

---

## Deployment

> **Complete hosting requirements:** See [HOSTING_REQUIREMENTS.md](HOSTING_REQUIREMENTS.md) for the Docker Compose setup, resource sizing, and the deployment checklist.

### Docker Deployment

#### Build API Image

```bash
docker build -f infrastructure/docker/Dockerfile.api -t seldonvigil-api:latest .
```

#### Build Worker Image

```bash
docker build -f infrastructure/docker/Dockerfile.worker -t seldonvigil-worker:latest .
```

#### Run with Docker Compose

Requires PostgreSQL (`localhost:5432`) running on the host (not containerised).
Redpanda runs in Docker Compose on `localhost:9092`.

```bash
cd infrastructure/docker
docker-compose up -d
```

### CI/CD Integration

#### GitHub Actions Example

```yaml
name: Deploy SeldonVigil

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Build Docker Image
        run: |
          docker build -f infrastructure/docker/Dockerfile.api -t seldonvigil-api:${{ github.sha }} .
      
      - name: Push to Registry
        run: |
          docker tag seldonvigil-api:${{ github.sha }} registry.acme.example/seldonvigil-api:${{ github.sha }}
          docker push registry.acme.example/seldonvigil-api:${{ github.sha }}
      
```

---

## Troubleshooting

### Common Issues

#### 1. Database Connection Failed

**Symptoms:** API fails to start with database connection error.

**Solutions:**
- Verify local PostgreSQL is running: `pg_isready -h localhost -p 5432`
- Check connection string in configuration
- Ensure database exists: `psql -h localhost -U seldonvigil -d seldonvigil`
- Check firewall rules

#### 2. Broker Connection Issues

**Symptoms:** Messages not being published/consumed.

**Solutions:**
- Verify Redpanda is reachable: `nc -zv localhost 9092`
- Check broker logs: `docker compose logs redpanda`
- Verify topic exists: `rpk topic list`
- Check consumer group status: `rpk group describe seldonvigil-workers`
- See `docs/runbooks/redpanda.md` for full troubleshooting

#### 3. Redis Cache Errors

**Symptoms:** Cache operations failing.

**Solutions:**
- Verify Redis is running: `docker-compose ps redis`
- Test Redis connection: `redis-cli -h localhost ping`
- Check memory limits in Redis configuration

#### 4. Authentication Failures

**Symptoms:** 401 Unauthorized errors.

**Solutions:**
- Verify token is not expired
- Check token is being sent in Authorization header
- Verify authority URL is correct
- Check audience matches configuration

#### 5. High Memory Usage

**Symptoms:** API or worker processes consuming excessive memory.

**Solutions:**
- Check for memory leaks in custom code
- Reduce trace sampling rate
- Implement pagination for large queries
- Increase container memory limits

#### 6. Slow Performance

**Symptoms:** API responses are slow.

**Solutions:**
- Check database query performance
- Enable caching for frequently accessed data
- Review trace data in your OTLP collector for bottlenecks
- Scale horizontally using Kubernetes

### Debug Mode

Enable debug logging:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Information"
      }
    }
  }
}
```

### Health Check Diagnostics

```bash
# Check overall health
curl http://localhost:8080/health

# Check liveness
curl http://localhost:8080/readyz

# Check readiness
curl http://localhost:8080/readyz
```

### Log Analysis

```bash
# View real-time logs
docker-compose logs -f api

# View worker logs
docker-compose logs -f worker

# Filter by log level
docker-compose logs api | grep ERROR

# View specific time range
docker-compose logs --since 2024-01-01T00:00:00 api
```

---

## Security Best Practices

### Production Security Checklist

- [ ] Remove development authentication endpoints
- [ ] Enable HTTPS/TLS for all endpoints
- [ ] Use managed secret management (Azure Key Vault, etc.)
- [ ] Implement rate limiting
- [ ] Enable audit logging
- [ ] Regular security audits
- [ ] Keep dependencies updated
- [ ] Implement network segmentation
- [ ] Use least privilege access
- [ ] Enable security headers

### Secret Management

#### Azure Key Vault Integration (optional)

Set `Azure__KeyVaultName` to load secrets from Azure Key Vault. When it is unset,
the API reads secrets from environment variables, so Key Vault is not required.

```csharp
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"];
builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultEndpoint),
    new DefaultAzureCredential());
```

#### Environment-Specific Secrets

Never commit secrets to version control. Use:

- Development: `.env` file (gitignored)
- Staging: Environment variables
- Production: Azure Key Vault / HashiCorp Vault

### Network Security

#### Firewall Rules

- Restrict database access to application nodes only
- Use VPC peering for inter-service communication
- Implement network policies in Kubernetes
- Enable TLS for all external connections

#### API Security

- Implement API versioning
- Use CORS policies appropriately
- Validate all input
- Implement request size limits
- Use security headers (CSP, X-Frame-Options, etc.)

### Data Protection

#### Encryption at Rest

- Enable PostgreSQL encryption
- Encrypt Redis data at rest
- Use encrypted volumes for persistent storage

#### Encryption in Transit

- TLS 1.3 for all connections
- Certificate pinning for external APIs
- Secure broker communication (TLS + SASL)

### Compliance

#### SOC2 Compliance

- Implement comprehensive audit logging
- Enable change tracking
- Regular access reviews
- Incident response procedures

#### GDPR Compliance

- Data minimization
- Right to be forgotten implementation
- Data portability
- Consent management

---

## Additional Resources

### Documentation

- [API Reference](reference/api/)
- [Architecture Documentation](architecture/stack-decisions.md)
- [Deployment Guides](HOSTING_REQUIREMENTS.md)
- [Runbooks](runbooks/redpanda.md)
- [Security Documentation](reference/modules/security.md)

### Support

- GitHub Issues: [https://github.com/ElenctiumLabs/SeldonVigil/issues](https://github.com/ElenctiumLabs/SeldonVigil/issues)

### Training

- Getting Started Guide
- Advanced Configuration
- Troubleshooting Workshop
- Security Best Practices

---

## Appendix

### Glossary

- **Agent** - An AI-powered software component that performs specific tasks
- **Trace** - A record of a single operation performed by an agent
- **Policy** - A rule or set of rules governing agent behavior
- **Budget** - A cost limit set for an agent or tenant
- **Saga** - A distributed transaction pattern for maintaining data consistency
- **Outbox** - A pattern for reliably publishing events to message brokers

### Configuration Reference

Full configuration reference available in the [Configuration](#configuration) section above.

### API Reference

Complete API documentation available at `/swagger` when running the API.

### Version History

See [CHANGELOG.md](../CHANGELOG.md) for version history and release notes.

---

**Last Updated:** January 2026  
**Version:** 1.0.0  
**Maintained By:** SeldonVigil Platform Team
