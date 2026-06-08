# ModelForge Rebuild Publish Script
# Regenerates publish/ artifacts for all three deployables.
# Usage: .\scripts\rebuild-publish.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root "publish"

Write-Host "=== ModelForge Rebuild Publish ===" -ForegroundColor Cyan
Write-Host ""

# Clean
if (Test-Path $publishDir) {
    Write-Host "Cleaning $publishDir..." -ForegroundColor Gray
    Remove-Item "$publishDir\Backend" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$publishDir\Sidecar" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$publishDir\Web" -Recurse -Force -ErrorAction SilentlyContinue
}

# Backend
Write-Host "Publishing Backend..." -ForegroundColor Yellow
dotnet publish "$root\src\backend\ModelForge.Backend\ModelForge.Backend.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output "$publishDir\Backend"
Write-Host "  -> publish/Backend/" -ForegroundColor Green

# Sidecar
Write-Host "Publishing Sidecar..." -ForegroundColor Yellow
dotnet publish "$root\src\sidecar\ModelForge.Sidecar\ModelForge.Sidecar.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output "$publishDir\Sidecar"
Write-Host "  -> publish/Sidecar/" -ForegroundColor Green

# Web Add-in
Write-Host "Building Web Add-in..." -ForegroundColor Yellow
Push-Location "$root\src\web"
npm run build
Pop-Location

if (Test-Path "$root\src\web\dist") {
    Copy-Item "$root\src\web\dist\*" "$publishDir\Web\" -Recurse -Force
    Write-Host "  -> publish/Web/" -ForegroundColor Green
}

# Build info
$buildInfo = @{
    product = "ModelForge"
    version = "0.2.0"
    configuration = "Release"
    runtime = "win-x64"
    builtAt = (Get-Date -Format "yyyyMMdd-HHmmss")
} | ConvertTo-Json

Set-Content (Join-Path $publishDir "build-info.json") -Value $buildInfo -Encoding UTF8

Write-Host ""
Write-Host "=== Publish complete ===" -ForegroundColor Cyan
Write-Host "Backend:  $publishDir\Backend\ModelForge.Backend.exe" -ForegroundColor White
Write-Host "Sidecar:  $publishDir\Sidecar\ModelForge.Sidecar.exe" -ForegroundColor White
Write-Host "Web:      $publishDir\Web\index.html" -ForegroundColor White
