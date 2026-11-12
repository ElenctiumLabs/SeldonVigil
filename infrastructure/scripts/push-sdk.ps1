<#
.SYNOPSIS
    Packs and pushes the SeldonVigil.Sdk NuGet package to NuGet.org.

.PARAMETER Version
    Override the version before packing (e.g. "1.1.0"). If omitted, uses the
    version already set in SeldonVigil.Sdk.csproj.

.PARAMETER Source
    NuGet source to push to. Defaults to nuget.org.

.PARAMETER ApiKey
    NuGet.org API key. Required unless -SkipPush is set.

.PARAMETER SkipPush
    Build and pack only — do not push.

.PARAMETER SkipBuild
    Skip the build step (useful if you already built and just want to push).

.EXAMPLE
    .\push-sdk.ps1 -ApiKey $env:NUGET_API_KEY       # pack + push current version
    .\push-sdk.ps1 -Version 1.2.0 -ApiKey $env:...   # bump to 1.2.0, pack, push
    .\push-sdk.ps1 -SkipPush                         # pack only, inspect the nupkg
#>
param(
    [string]$Version,
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$ApiKey,
    [switch]$SkipPush,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$root       = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sdkCsproj  = Join-Path $root "src\SeldonVigil.Sdk\SeldonVigil.Sdk.csproj"
$publishDir = Join-Path $root "artifacts\packages"

# ── 1. Read current version ────────────────────────────────────────────
$content = Get-Content $sdkCsproj -Raw
$match = [regex]::Match($content, '<Version>([^<]+)</Version>')
if (-not $match.Success) {
    Write-Error "Could not read <Version> from $sdkCsproj"
    exit 1
}
$currentVersion = $match.Groups[1].Value

$effectiveVersion = if ($Version) { $Version } else { $currentVersion }
Write-Host "SDK Package:  SeldonVigil.Sdk" -ForegroundColor Cyan
Write-Host "Version:      $effectiveVersion"  -ForegroundColor Cyan
Write-Host "Source:       $Source"             -ForegroundColor Cyan
Write-Host ""

# ── 2. Bump version in csproj (if override provided) ───────────────────
if ($Version -and $Version -ne $currentVersion) {
    Write-Host "Bumping version $currentVersion -> $Version in csproj..." -ForegroundColor Yellow
    $content = Get-Content $sdkCsproj -Raw
    $content = $content -replace "<Version>$([regex]::Escape($currentVersion))</Version>", "<Version>$Version</Version>"
    Set-Content $sdkCsproj $content -NoNewline

    Write-Host "  Updated SeldonVigil.Sdk.csproj" -ForegroundColor Green
}

# ── 3. Restore ──────────────────────────────────────────────────────────
Write-Host "`nRestoring SDK..." -ForegroundColor Yellow
dotnet restore $sdkCsproj
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet restore failed"; exit 1 }

# ── 4. Build ────────────────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "`nBuilding SDK (Release)..." -ForegroundColor Yellow
    dotnet build $sdkCsproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed"; exit 1 }
}

# ── 5. Pack ─────────────────────────────────────────────────────────────
Write-Host "`nPacking SDK..." -ForegroundColor Yellow
if (-not (Test-Path $publishDir)) { New-Item -ItemType Directory -Path $publishDir -Force | Out-Null }

dotnet pack $sdkCsproj -c Release --no-build -o $publishDir
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet pack failed"; exit 1 }

$nupkg = Get-ChildItem "$publishDir\SeldonVigil.Sdk.$effectiveVersion.nupkg" -ErrorAction SilentlyContinue
if (-not $nupkg) {
    $nupkg = Get-ChildItem "$publishDir\SeldonVigil.Sdk.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
if (-not $nupkg) { Write-Error "No .nupkg found in $publishDir"; exit 1 }

Write-Host "  Created: $($nupkg.Name)" -ForegroundColor Green

# ── 6. Push ─────────────────────────────────────────────────────────────
if (-not $SkipPush) {
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        Write-Error "An API key is required for nuget.org. Pass -ApiKey or set `$env:NUGET_API_KEY."
        exit 1
    }

    Write-Host "`nPushing to nuget.org..." -ForegroundColor Yellow
    dotnet nuget push $nupkg.FullName --source $Source --api-key $ApiKey --skip-duplicate
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet nuget push failed"; exit 1 }
    Write-Host "  Pushed successfully!" -ForegroundColor Green
}

Write-Host "`nDone." -ForegroundColor Cyan
