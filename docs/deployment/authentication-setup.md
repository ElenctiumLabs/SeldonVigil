# SeldonVigil - Production Authentication Setup Guide

This guide explains how to configure authentication for production deployments of SeldonVigil.

## Overview

SeldonVigil uses OpenID Connect (OIDC) for authentication. The platform requires an external OIDC provider to issue and validate JWT tokens. Development mode includes a fallback token endpoint for testing, but this is **disabled in production builds**.

## Required Configuration

### 1. OIDC Provider Configuration

You must configure an OIDC provider such as:
- Auth0
- Azure Active Directory (Entra ID)
- Okta
- Keycloak
- Any other OIDC-compliant provider

### 2. Required Environment Variables

Add the following to your `appsettings.Production.json` or environment variables:

```json
{
  "Authentication": {
    "Authority": "https://oidc.acme.example/.well-known/openid-configuration",
    "Audience": "your-api-audience"
  },
  "Azure": {
    "KeyVaultName": "your-keyvault-name"
  }
}
```

### 3. Connection Strings

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=your-postgres-server;Database=seldonvigil;Username=postgres;Password=your-password",
    "Redis": "your-redis-connection-string",
    "Broker": "your-broker-bootstrap-servers"
  }
}
```

## Provider-Specific Setup

### Auth0

1. Create an Auth0 account and application
2. Set the application type to "Regular Web Application"
3. Configure allowed callbacks:
   - `https://app.acme.example/callback`
   - `https://app.acme.example/silent-callback`
4. Configure allowed logout URLs:
   - `https://app.acme.example`
5. Get your Auth0 domain and audience

**Configuration:**
```json
{
  "Authentication": {
    "Authority": "https://your-tenant.auth0.com/.well-known/openid-configuration",
    "Audience": "https://your-api-identifier"
  }
}
```

### Azure Active Directory (Entra ID)

1. Register an application in Azure AD
2. Add a web platform with redirect URI: `https://app.acme.example/signin-oidc`
3. Enable ID tokens
4. Copy the Application (client) ID and Tenant ID
5. Generate a client secret

**Configuration:**
```json
{
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration",
    "Audience": "{client-id}"
  }
}
```

### Okta

1. Create an Okta application (SPA or Web)
2. Configure allowed redirect URIs
3. Copy the Okta domain and client ID

**Configuration:**
```json
{
  "Authentication": {
    "Authority": "https://your-okta-domain.okta.com/.well-known/openid-configuration",
    "Audience": "api://default"
  }
}
```

### Keycloak

1. Create a Keycloak realm
2. Create a confidential client
3. Configure valid redirect URIs
4. Copy the realm URL and client ID

**Configuration:**
```json
{
  "Authentication": {
    "Authority": "https://keycloak.acme.example/realms/your-realm/.well-known/openid-configuration",
    "Audience": "your-client-id"
  }
}
```

## Azure KeyVault Setup (optional)

Key Vault is optional. When `Azure:KeyVaultName` is unset, the API reads secrets
from environment variables. Set it only if you want secrets stored in Key Vault.

### 1. Create KeyVault

```bash
az keyvault create \
  --name "your-keyvault-name" \
  --resource-group "your-resource-group" \
  --location "your-location"
```

### 2. Add Secrets

```bash
# PostgreSQL connection string
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "ConnectionStrings--PostgreSQL" \
  --value "Host=your-postgres-server;Database=seldonvigil;..."

# Redis connection string
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "ConnectionStrings--Redis" \
  --value "your-redis-connection-string"

# Broker connection string
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "ConnectionStrings--Broker" \
  --value "your-broker-bootstrap-servers"

# OIDC Authority
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "Authentication--Authority" \
  --value "https://oidc.acme.example/.well-known/openid-configuration"

# OIDC Audience
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "Authentication--Audience" \
  --value "your-api-audience"
```

### 3. Configure Access

```bash
# Get the managed identity principal ID
az identity show \
  --name "your-managed-identity" \
  --resource-group "your-resource-group" \
  --query principalId -o tsv

# Grant access to KeyVault
az keyvault set-policy \
  --name "your-keyvault-name" \
  --object-id "your-principal-id" \
  --secret-permissions get list
```

## Configuration Validation

The application validates configuration at startup. If validation fails, the application will not start with an error message indicating which configuration is missing.

### Validation Checks

- `Authentication:Authority` - Required in all environments
- `Authentication:Audience` - Required in all environments
- `Azure:KeyVaultName` - Optional; set to load secrets from Azure Key Vault
- `ConnectionStrings:PostgreSQL` - Required in all environments
- `ConnectionStrings:Redis` - Required in all environments
- `ConnectionStrings:Broker` - Required in all environments

## Health Check

The application includes an authentication health check at `/healthz` that validates:
- OIDC authority is configured
- OIDC audience is configured
- OIDC discovery endpoint is accessible

**Check health:**
```bash
curl https://app.acme.example/healthz
```

## Token Validation

The application validates JWT tokens using:
- Issuer validation against configured authority
- Audience validation against configured audience
- Lifetime validation (with 5-minute clock skew tolerance)
- Signing key validation against OIDC provider's JWKS endpoint

## Development Mode

In development mode, you can use the `/dev/token` endpoint to issue test JWT tokens. This endpoint:
- Is only available in DEBUG builds
- Is only available when `ASPNETCORE_ENVIRONMENT=Development`
- Uses a symmetric key for signing (NOT for production)
- Logs a warning when enabled

**Get a dev token:**
```bash
curl http://localhost:5000/dev/token
```

**Customize token:**
```bash
curl http://localhost:5000/dev/token?hours=8&role=Administrator&sub=dev-user-123
```

## Security Best Practices

1. **Never use dev auth in production** - The `/dev/token` endpoint is compiled out in release builds
2. **Use a secret manager for production secrets** - Azure Key Vault, HashiCorp Vault, AWS Secrets Manager, or your deployment platform's secret store
3. **Rotate signing keys** - Your OIDC provider should rotate signing keys automatically
4. **Use short-lived tokens** - Configure your OIDC provider to issue tokens with short expiration
5. **Enable token revocation** - Configure your OIDC provider to support token revocation
6. **Monitor authentication failures** - Set up alerts for authentication health check failures
7. **Use HTTPS only** - Never expose authentication endpoints over HTTP

## Troubleshooting

See [Authentication Troubleshooting Guide](./troubleshooting-authentication.md) for common issues and solutions.
