param(
    [string]$MsiPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "ModelForge.msi"),
    [string]$LogDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\manual-e2e"),
    [switch]$SkipUninstall
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message"
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-MsiExec {
    param(
        [Parameter(Mandatory = $true)][ValidateSet("Install", "Uninstall")][string]$Action,
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [Parameter(Mandatory = $true)][string]$LogPath
    )

    $arguments = if ($Action -eq "Install") {
        @("/i", $PackagePath, "/qn", "/norestart", "/l*v", $LogPath)
    }
    else {
        @("/x", $PackagePath, "/qn", "/norestart", "/l*v", $LogPath)
    }

    Write-Step "$Action MSI: $PackagePath"
    Write-Host "msiexec.exe $($arguments -join ' ')"
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "$Action failed with exit code $($process.ExitCode). See log: $LogPath"
    }
}

function Invoke-HealthCheck {
    param([Parameter(Mandatory = $true)][string]$Uri)

    for ($attempt = 1; $attempt -le 20; $attempt++) {
        try {
            Invoke-RestMethod -Method Get -Uri $Uri -TimeoutSec 5 | Out-Null
            Write-Step "Health check passed: $Uri"
            return
        }
        catch {
            if ($attempt -eq 20) {
                throw "Health check failed after 20 attempts: $Uri. $($_.Exception.Message)"
            }
            Start-Sleep -Seconds 1
        }
    }
}

function Get-ServiceState {
    param([Parameter(Mandatory = $true)][string]$Name)

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        return "missing"
    }

    return $service.Status.ToString()
}

if (-not (Test-IsAdministrator)) {
    throw "This script must run from an elevated PowerShell session because ModelForge.msi is a per-machine installer."
}

$resolvedMsi = (Resolve-Path -LiteralPath $MsiPath).Path
$resolvedLogDir = if (Test-Path -LiteralPath $LogDir) {
    (Resolve-Path -LiteralPath $LogDir).Path
}
else {
    (New-Item -ItemType Directory -Force -Path $LogDir).FullName
}

$installLog = Join-Path $resolvedLogDir "msi-install-admin.log"
$uninstallLog = Join-Path $resolvedLogDir "msi-uninstall-admin.log"

Write-Step "Using MSI: $resolvedMsi"
Write-Step "Writing logs to: $resolvedLogDir"

Invoke-MsiExec -Action Install -PackagePath $resolvedMsi -LogPath $installLog

Write-Step "Checking services after install"
$sidecarState = Get-ServiceState -Name "ModelForge.Sidecar"
$backendState = Get-ServiceState -Name "ModelForge.Backend"
Write-Host "ModelForge.Sidecar: $sidecarState"
Write-Host "ModelForge.Backend: $backendState"

if ($sidecarState -ne "Running") {
    throw "ModelForge.Sidecar is not running after install. State: $sidecarState"
}

if ($backendState -ne "Running") {
    throw "ModelForge.Backend is not running after install. State: $backendState"
}

Invoke-HealthCheck -Uri "http://localhost:5200/health"
Invoke-HealthCheck -Uri "http://localhost:5095/health"

$installDir = "C:\Program Files\ModelForge"
if (-not (Test-Path -LiteralPath $installDir)) {
    throw "Install directory not found: $installDir"
}
Write-Step "Install directory exists: $installDir"

if (-not $SkipUninstall) {
    Invoke-MsiExec -Action Uninstall -PackagePath $resolvedMsi -LogPath $uninstallLog

    Write-Step "Checking services after uninstall"
    $sidecarAfterUninstall = Get-ServiceState -Name "ModelForge.Sidecar"
    $backendAfterUninstall = Get-ServiceState -Name "ModelForge.Backend"
    Write-Host "ModelForge.Sidecar: $sidecarAfterUninstall"
    Write-Host "ModelForge.Backend: $backendAfterUninstall"

    if ($sidecarAfterUninstall -ne "missing") {
        throw "ModelForge.Sidecar still exists after uninstall. State: $sidecarAfterUninstall"
    }

    if ($backendAfterUninstall -ne "missing") {
        throw "ModelForge.Backend still exists after uninstall. State: $backendAfterUninstall"
    }
}

Write-Step "MSI admin regression completed"
