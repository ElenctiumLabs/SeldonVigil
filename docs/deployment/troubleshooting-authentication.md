# SeldonVigil - Authentication Troubleshooting Guide

This guide helps diagnose and resolve common authentication issues in SeldonVigil.

## Common Issues

### Issue: "Authentication:Authority must be configured"

**Symptom:** Application fails to start with error:
```
Invalid configuration: Authentication:Authority must be configured in appsettings.
```

**Cause:** The OIDC authority is not configured in your application settings.

**Solution:**
1. Add `Authentication:Authority` to your `appsettings.json` or environment variables
2. Ensure the authority URL points to your OIDC provider's discovery endpoint
3. Example: `https://oidc.acme.example/.well-known/openid-configuration`

**Example Configuration:**
```json
{
  "Authentication": {
    "Authority": "https://your-tenant.auth0.com/.well-known/openid-configuration",
    "Audience": "your-api-audience"
  }
}
```

---

### Issue: "OIDC discovery endpoint returned 404"

**Symptom:** Health check shows degraded status:
```
OIDC discovery endpoint returned 404
```

**Cause:** The OIDC discovery endpoint URL is incorrect or the provider is down.

**Solution:**
1. Verify your authority URL is correct
2. Ensure the URL ends with `/.well-known/openid-configuration`
3. Test the URL in a browser or with curl:
   ```bash
   curl https://oidc.acme.example/.well-known/openid-configuration
   ```
4. Check your OIDC provider status

**Correct Authority URLs:**
- Auth0: `https://your-tenant.auth0.com/.well-known/openid-configuration`
- Azure AD: `https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration`
- Okta: `https://your-okta-domain.okta.com/.well-known/openid-configuration`
- Keycloak: `https://keycloak.acme.example/realms/{realm}/.well-known/openid-configuration`

---

### Issue: "Failed to connect to OIDC discovery endpoint"

**Symptom:** Health check shows degraded status with connection error.

**Cause:** Network connectivity issues or firewall blocking the OIDC provider.

**Solution:**
1. Check network connectivity from your application server to the OIDC provider
2. Verify firewall rules allow outbound HTTPS traffic
3. Check if your OIDC provider has IP allowlists that need your server's IP
4. Test connectivity:
   ```bash
   curl -v https://oidc.acme.example/.well-known/openid-configuration
   ```

---

### Issue: "JWT validation failed: Invalid issuer"

**Symptom:** API requests fail with 401 Unauthorized and logs show issuer validation error.

**Cause:** The token's issuer doesn't match the configured authority.

**Solution:**
1. Verify your `Authentication:Authority` matches the token issuer
2. Check the token's `iss` claim:
   ```bash
   # Decode JWT to see claims
   echo "your-jwt-token" | jq -R 'split(".") | .[1] | @base64d | fromjson'
   ```
3. Ensure your OIDC provider is issuing tokens with the correct issuer

---

### Issue: "JWT validation failed: Invalid audience"

**Symptom:** API requests fail with 401 Unauthorized and logs show audience validation error.

**Cause:** The token's audience doesn't match the configured audience.

**Solution:**
1. Verify your `Authentication:Audience` matches the token's audience
2. Check the token's `aud` claim
3. Ensure your OIDC provider is configured to issue tokens with the correct audience
4. For Auth0, ensure the API identifier matches your audience configuration

---

### Issue: "JWT validation failed: Token expired"

**Symptom:** API requests fail with 401 Unauthorized and logs show token expiration error.

**Cause:** The JWT token has expired.

**Solution:**
1. Implement token refresh in your client application
2. Configure your OIDC provider to issue longer-lived tokens if needed
3. Ensure your client application handles token expiration gracefully
4. Check clock synchronization between servers (5-minute skew tolerance is configured)

---

### Issue: "/dev/token endpoint not found"

**Symptom:** Development token endpoint returns 404 in development.

**Cause:** The endpoint is only available in DEBUG builds and Development environment.

**Solution:**
1. Ensure you're building in Debug configuration:
   ```bash
   dotnet build --configuration Debug
   ```
2. Ensure `ASPNETCORE_ENVIRONMENT=Development` is set
3. The endpoint is intentionally unavailable in Release builds for security

**Note:** This endpoint should never be used in production.

---

### Issue: "Azure KeyVault access denied"

**Symptom:** Application fails to start with KeyVault access error.

**Cause:** The application's managed identity doesn't have permission to access KeyVault.

**Solution:**
1. Verify your managed identity exists:
   ```bash
   az identity show --name "your-identity" --resource-group "your-rg"
   ```
2. Grant the managed identity access to KeyVault:
   ```bash
   az keyvault set-policy \
     --name "your-keyvault" \
     --object-id "your-principal-id" \
     --secret-permissions get list
   ```
3. Ensure the identity is assigned to your application resource (VM, App Service, etc.)

---

### Issue: "Connection string not found"

**Symptom:** Application fails to start with connection string validation error.

**Cause:** Required connection strings are not configured.

**Solution:**
1. Add connection strings to your configuration:
   ```json
   {
     "ConnectionStrings": {
       "PostgreSQL": "Host=your-postgres;Database=seldonvigil;...",
       "Redis": "your-redis-connection-string",
       "Broker": "your-redpanda-broker:9092"
     }
   }
   ```
2. For production, store these in Azure KeyVault
3. For development, use User Secrets or `appsettings.Development.json`

---

## Debugging Steps

### 1. Check Configuration

View current configuration:
```bash
# View appsettings
cat appsettings.Production.json

# View environment variables
env | grep -i "Authentication\|ConnectionStrings\|Azure"

# View KeyVault secrets (if accessible)
az keyvault secret list --vault-name "your-keyvault"
```

### 2. Check Health Status

```bash
# Full health check
curl https://app.acme.example/healthz

# Readiness check
curl https://app.acme.example/readyz
```

### 3. Test OIDC Discovery

```bash
# Test discovery endpoint
curl https://oidc.acme.example/.well-known/openid-configuration

# Test JWKS endpoint (from discovery response)
curl https://oidc.acme.example/.well-known/jwks.json
```

### 4. Validate Token

```bash
# Decode and inspect JWT
echo "your-jwt-token" | jq -R 'split(".") | .[1] | @base64d | fromjson'

# Check issuer and audience
echo "your-jwt-token" | jq -R 'split(".") | .[1] | @base64d | fromjson | {iss, aud, exp}'
```

### 5. Check Application Logs

```bash
# Docker logs
docker compose logs api

# Local development logs
# Check console output
```

## Health Check Status Codes

- **Healthy**: All authentication configuration is valid and OIDC endpoint is accessible
- **Degraded**: OIDC endpoint is slow or temporarily unavailable, but tokens may still validate
- **Unhealthy**: Critical configuration is missing or OIDC endpoint is completely unavailable

## Getting Help

If you continue to experience issues:

1. Check the [Authentication Setup Guide](./authentication-setup.md)
2. Review the OIDC provider's documentation
3. Check Azure KeyVault logs for access issues
4. Enable detailed logging in your application
5. Contact your infrastructure team for network/firewall issues

## Security Reminders

- Never share your JWT tokens or secrets
- Never use the `/dev/token` endpoint in production
- Always use HTTPS for authentication endpoints
- Regularly rotate your OIDC provider signing keys
- Monitor authentication health check failures
- Review audit logs for authentication anomalies
