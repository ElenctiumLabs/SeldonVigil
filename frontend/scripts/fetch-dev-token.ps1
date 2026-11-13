param(
	[string]$Url = "http://localhost:8080",
	[int]$Hours = 24,
	[string]$Role = "admin",
	[string]$Sub = "dev-user",
	[string]$OutFile = ".devtoken"
)

Write-Host "Fetching dev token from $Url/dev/token?hours=$Hours&role=$Role&sub=$Sub"

# Accept self-signed certs for local development
add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
	public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) { return true; }
}
"@ -ErrorAction SilentlyContinue
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

try {
	$uri = "$Url/dev/token?hours=$Hours&role=$Role&sub=$Sub"
	$resp = Invoke-RestMethod -Uri $uri -Method Get -UseBasicParsing -ErrorAction Stop
	if ($null -eq $resp.token) {
		Write-Error "Unexpected response: $($resp | ConvertTo-Json -Depth 3)"
		exit 1
	}
	$token = $resp.token
	Set-Content -Path $OutFile -Value $token -Force
	Write-Host "Saved token to $OutFile"
	Write-Host "Use this token as the Authorization header:"
	Write-Host "  Authorization: Bearer $token"
	Write-Host "Example curl:"
	Write-Host "  curl -H \"Authorization: Bearer $token\" https://localhost:61993/api/v1/observability/traces?limit=10 -k"
} catch {
	Write-Error "Failed to fetch token: $_"
	exit 1
}
