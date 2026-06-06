<#
.SYNOPSIS
    ModelForge Phase D build script — publish Sidecar + Backend + Web

.DESCRIPTION
    Builds all ModelForge components as self-contained deployments prepared
    for MSI/WiX packaging. Outputs to publish/ directory.

.PARAMETER Configuration
    Build configuration (Debug/Release). Default: Release.

.PARAMETER Runtime
    Target runtime identifier. Default: win-x64.

.EXAMPLE
    .\scripts\build-installer.ps1
    .\scripts\build-installer.ps1 -Configuration Debug
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $root "publish"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "=== ModelForge Build Installer ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration  Runtime: $Runtime"
Write-Host "Output: $publishDir"
Write-Host ""

# Clean publish directory
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

# 1. Build Sidecar (self-contained)
Write-Host "[1/4] Building Sidecar..." -ForegroundColor Yellow
dotnet publish "$root\src\sidecar\ModelForge.Sidecar\ModelForge.Sidecar.csproj" `
    -c $Configuration -r $Runtime --self-contained `
    -o "$publishDir\Sidecar" | Out-Null
Write-Host "  -> Sidecar built: $publishDir\Sidecar\ModelForge.Sidecar.exe"

# 2. Build Backend (self-contained)
Write-Host "[2/4] Building Backend..." -ForegroundColor Yellow
dotnet publish "$root\src\backend\ModelForge.Backend\ModelForge.Backend.csproj" `
    -c $Configuration -r $Runtime --self-contained `
    -o "$publishDir\Backend" | Out-Null
Write-Host "  -> Backend built: $publishDir\Backend\ModelForge.Backend.exe"

# 3. Build Web Add-in (npm)
Write-Host "[3/4] Building Web Add-in..." -ForegroundColor Yellow
Push-Location "$root\src\web"
try {
    npm run build 2>&1 | Out-Null
    if (Test-Path "dist") {
        Copy-Item -Recurse "dist\*" "$publishDir\Web" -Force
        # Also copy function-file.html for Office add-in commands
        if (Test-Path "publicunction-file.html") {
            Copy-Item "publicunction-file.html" "$publishDir\Web" -Force
        }
        Write-Host "  -> Web built: $publishDir\Web"
    } else {
        Write-Warning "  -> npm build did not produce dist/ directory"
    }
} finally {
    Pop-Location
}

# 4. Copy manifest + scripts
Write-Host "[4/4] Copying assets..." -ForegroundColor Yellow
Copy-Item -Path "$root\manifest" -Destination "$publishDir\manifest" -Recurse
Copy-Item "$root\scripts\dev-backend.cmd" -Destination "$publishDir"
Copy-Item "$root\scripts\dev-web.cmd" -Destination "$publishDir"


# 5. Build MSI with WiX
Write-Host '[5/5] Building MSI with WiX...' -ForegroundColor Yellow
try {
    wix build 'installer\ModelForge.Installer\Package.wxs' -o '\ModelForge.msi' 2>&1 | Out-Null
    if (Test-Path 'D:\ModelForge.msi') {
        $size = (Get-Item 'D:\ModelForge.msi').Length / 1KB
        Write-Host "  -> MSI built: D:\ModelForge.msi ($([int]$size) KB)" -ForegroundColor Green
    }
} catch {
    Write-Warning "WiX build failed. Install WiX Toolset v5: dotnet tool install -g wix"
}
# Write build info
@"
{
  "product": "ModelForge",
  "version": "0.1.0",
  "configuration": "$Configuration",
  "runtime": "$Runtime",
  "builtAt": "$stamp"
}
"@ | Out-File -FilePath "$publishDir\build-info.json" -Encoding UTF8

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host "Output: $publishDir"
Write-Host ""
Write-Host "To install Sidecar as Windows Service:"
Write-Host "  sc create ModelForge.Sidecar binPath= `"$publishDir\Sidecar\ModelForge.Sidecar.exe`" start=auto"
Write-Host "  sc start ModelForge.Sidecar"
Write-Host ""
Write-Host "To sideload Web Add-in in Excel:"
Write-Host "  Copy $publishDir\manifest\modelForge.web.xml to a trusted catalog folder"
